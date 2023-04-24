using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Text;
using System.IO;
using System.Globalization;

public static class Commands
{
    public static List<Command> GetAllCommands()
    {
        List<Command> output = new List<Command>();

        output.Add(new ClearCommand());
        output.Add(new GenerateCommand());
        output.Add(new HelpCommand());
        output.Add(new LoadCommand());
        output.Add(new RenderOptionsCommand());
        output.Add(new RenderOptions3DCommand());
        output.Add(new SaveCommand());

        return output;
    }

    public static Command GetCommand(string input)
    {
        Command command = GetAllCommands().FirstOrDefault((c) => c.IsThis(input));
        if (command == null)
        {
            CommandLine.INSTANCE.LogError(
                $"{input.Split(' ')[0].ToUpper()} is not recognized as a valid command.\nType help to get a list of valid commands."
            );
            return null;
        }
        return command;
    }

    public static void ExecuteCommand(string input)
    {
        GetCommand(input.Split(' ')[0])?.Execute(input.Split(' '));
    }
}

public class Command
{
    public virtual bool IsThis(string input)
    {
        return input.ToUpper().Split(' ')[0].Equals(GetName().ToUpper());
    }

    public virtual void Execute(params string[] @params) { }

    public virtual string GetHelpString()
    {
        if (GetSyntaxHelp().Equals(""))
            return GetShortDescription();
        return GetShortDescription() + "\n" + GetSyntaxHelp();
    }

    public virtual string GetShortDescription()
    {
        return "No short description specified.";
    }

    public virtual string GetSyntaxHelp()
    {
        return "";
    }

    public virtual string GetName()
    {
        return this.ToString();
    }
}

public class ClearCommand : Command
{
    public override void Execute(params string[] @params)
    {
        if (@params.Count() > 1)
        {
            CommandLine.INSTANCE.LogError(
                $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
            );
            return;
        }
        CommandLine.INSTANCE.ClearOutput();
    }

    public override string GetShortDescription()
    {
        return "Clears the output field.";
    }

    public override string GetName()
    {
        return "clear";
    }
}

public class GenerateCommand : Command
{
    public override void Execute(params string[] @params)
    {
        if (@params.Count() != 4 && @params.Count() != 5)
        {
            CommandLine.INSTANCE.LogError(
                $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
            );
            return;
        }
        if (!uint.TryParse(@params[1], NumberStyles.Any, CultureInfo.InvariantCulture, out uint IntegralDetail))
        {
            CommandLine.INSTANCE.LogError(
                $"{@params[1].ToUpper()} has not been recognized as a valid natural number."
            );
            return;
        }
        if (!int.TryParse(@params[2], NumberStyles.Any, CultureInfo.InvariantCulture, out int start))
        {
            CommandLine.INSTANCE.LogError(
                $"{@params[2].ToUpper()} has not been recognized as a valid integer value."
            );
            return;
        }
        if (!int.TryParse(@params[3], NumberStyles.Any, CultureInfo.InvariantCulture, out int end))
        {
            CommandLine.INSTANCE.LogError(
                $"{@params[3].ToUpper()} has not been recognized as a valid integer value."
            );
            return;
        }
        if (@params.Count() == 4 && CameraScript.INSTANCE.svg == null)
        {
            CommandLine.INSTANCE.LogError(
                $"No SVG is currently loaded, please load an SVG using >load or specify a path using [path] and try again."
            );
            return;
        }
        if (@params.Count() == 5 && !File.Exists(@params[4]))
        {
            CommandLine.INSTANCE.LogError($"{@params[1]} is not a valid path.");
            return;
        }
        CameraScript.INSTANCE.IntegralDetail = IntegralDetail;
        CameraScript.INSTANCE.StartIndex = start;
        CameraScript.INSTANCE.StopIndex = end;
        if (@params.Count() == 4)
        {
            CameraScript.INSTANCE.LoadFourier(
                FourierGenerator.Generate(CameraScript.INSTANCE.svg),
                CameraScript.INSTANCE.BL,
                CameraScript.INSTANCE.TR
            );
        }
        else
        {
            SVG svg = SVGReader.ProcessPath(@params[4]);
            CameraScript.INSTANCE.LoadFourier(FourierGenerator.Generate(svg), svg.BL, svg.TR);
        }
    }

    public override string GetShortDescription()
    {
        return "Generates a Fourier series from an SVG file and loads it to the renderer.";
    }

    public override string GetSyntaxHelp()
    {
        return ">generate <accuracy> <start> <end> [path]\n    - accuracy: accuracy to approximate the SVG file with.\n    - start: starting coefficient of the vectors of the Fourier series.\n    - end: ending coefficient of the vectors of the Fourier series.\n    - path: absolute path of the file to generate a Fourier series from, if none is specified, the currently loaded SVG is used.";
    }

    public override string GetName()
    {
        return "generate";
    }
}

public class HelpCommand : Command
{
    public override void Execute(params string[] @params)
    {
        switch (@params.Length)
        {
            case 1:
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < Commands.GetAllCommands().Count; i++)
                {
                    if (i != 0)
                        builder.Append("\n");
                    builder.Append(
                        Commands.GetAllCommands()[i].GetName().ToUpper()
                            + " - "
                            + Commands.GetAllCommands()[i].GetShortDescription()
                    );
                }
                CommandLine.INSTANCE.LogText(builder.ToString());
                break;
            case 2:
                Command command = Commands.GetCommand(@params[1]);
                if (command != null)
                    CommandLine.INSTANCE.LogText(Commands.GetCommand(@params[1]).GetHelpString());
                break;
            default:
                CommandLine.INSTANCE.LogError(
                    $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
                );
                break;
        }
    }

    public override string GetShortDescription()
    {
        return "Returns info on all commands.";
    }

    public override string GetSyntaxHelp()
    {
        return ">help [command]\n    - command: returns info on the specified command.";
    }

    public override string GetName()
    {
        return "help";
    }
}

public class LoadCommand : Command
{
    public override void Execute(params string[] @params)
    {
        if (@params.Count() != 3)
        {
            CommandLine.INSTANCE.LogError(
                $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
            );
            return;
        }
        if (!File.Exists(@params[1]))
        {
            CommandLine.INSTANCE.LogError($"{@params[1]} is not a valid path.");
            return;
        }
        Debug.Log("File exists");
        if (@params[2].ToUpper().Equals("SVG"))
        {
            CameraScript.INSTANCE.LoadSVG(SVGReader.ProcessPath(@params[1]));
        }
        else if (@params[2].ToUpper().Equals("OBJ"))
        {
            CameraScript.INSTANCE.LoadOBJ(OBJReader.ProcessPath(@params[1]));
        }
        else if (@params[2].ToUpper().Equals("FOURIER"))
        {
            CameraScript.INSTANCE.LoadFourier(
                FourierGenerator.Load(@params[1]),
                CameraScript.INSTANCE.BL,
                CameraScript.INSTANCE.TR
            );
        }
        else
        {
            CommandLine.INSTANCE.LogError(
                $"{@params[2].ToUpper()} has not been recognized as a valid argument for <SVG | FOURIER>."
            );
            return;
        }
    }

    public override string GetShortDescription()
    {
        return "Loads a file into the renderer.";
    }

    public override string GetSyntaxHelp()
    {
        return ">load <path> <SVG | OBJ | FOURIER>\n    - path: absolute path of the file.\n    - SVG: load path as SVG.\n    - OBJ: load path as OBJ.\n    - FOURIER: load path as Fourier Series.";
    }

    public override string GetName()
    {
        return "load";
    }
}

public class RenderOptionsCommand : Command
{
    public override void Execute(params string[] @params)
    {
        if (@params.Count() < 2)
        {
            CommandLine.INSTANCE.LogError(
                $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
            );
            return;
        }
        Vector2 BL = CameraScript.INSTANCE.BL;
        Vector2 TR = CameraScript.INSTANCE.TR;
        bool gridLines = false;
        bool axis = false;
        bool svg = false;
        bool obj = false;
        bool fourier = false;
        bool trail = false;
        if (@params[1].Contains(',') && @params[1].Split(',').Count() == 2)
        {
            if (
                (
                    !float.TryParse(@params[1].Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out BL.x)
                    && @params[1].Split(',')[0] != ""
                )
                || (
                    !float.TryParse(@params[1].Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out BL.y)
                    && @params[1].Split(',')[1] != ""
                )
            )
            {
                CommandLine.INSTANCE.LogError(
                    $"{@params[1]} has not been recognized as a valid coordinate."
                );
                return;
            }
            BL.x = @params[1].Split(',')[0] == "" ? CameraScript.INSTANCE.BL.x : BL.x;
            BL.y = @params[1].Split(',')[1] == "" ? CameraScript.INSTANCE.BL.y : BL.y;
        }
        if (@params.Count() >= 3 && @params[2].Split(',').Count() == 2)
        {
            if (
                (
                    (
                        !float.TryParse(@params[2].Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out TR.x)
                        && @params[2].Split(',')[0] != ""
                    )
                    || (
                        !float.TryParse(@params[2].Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out TR.y)
                        && @params[2].Split(',')[1] != ""
                    )
                )
            )
            {
                CommandLine.INSTANCE.LogError(
                    $"{@params[2]} has not been recognized as a valid coordinate."
                );
                return;
            }
            TR.x = @params[2].Split(',')[0] == "" ? CameraScript.INSTANCE.TR.x : TR.x;
            TR.y = @params[2].Split(',')[1] == "" ? CameraScript.INSTANCE.TR.y : TR.y;
        }
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("GRIDLINES")) != default(string))
            gridLines = true;
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("AXIS")) != default(string))
            axis = true;
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("SVG")) != default(string))
            svg = true;
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("OBJ")) != default(string))
            obj = true;
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("FOURIER")) != default(string))
            fourier = true;
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("TRAIL")) != default(string))
            trail = true;

        CameraScript.INSTANCE.BL = BL;
        CameraScript.INSTANCE.TR = TR;
        CameraScript.INSTANCE.Gridlines = gridLines;
        CameraScript.INSTANCE.Axis = axis;
        CameraScript.INSTANCE.Svg = svg;
        CameraScript.INSTANCE.Obj = obj;
        CameraScript.INSTANCE.Fourier = fourier;
        CameraScript.INSTANCE.Trail = trail;

        PerspectiveRenderer.INSTANCE.Matrix = PerspectiveRenderer.INSTANCE.MatrixGen();
    }

    public override string GetShortDescription()
    {
        return "Changes the options for the renderer.";
    }

    public override string GetSyntaxHelp()
    {
        return ">render [x1,y1] [x2,y2] [GridLines] [Axis] [SVG] [OBJ] [Fourier] [Trail]\n    x1,y1 - Coordinates of the bottom left corner. If none are specified, the current coordinates are kept.\n    x2,y2 Coordinates of the top right corner. If none are specified, the current coordinates are kept.\n    - GridLines: Render gridlines every unit.\n    - Axis: Render axis.\n    - SVG: render SVG.\n    - OBJ: render OBJ.\n    - Fourier: Render the Fourier series.\n    - Trail: Render the trail from the Fourier series.";
    }

    public override string GetName()
    {
        return "render";
    }
}

public class RenderOptions3DCommand : Command
{
    public override void Execute(params string[] @params)
    {
        if (@params.Count() < 2)
        {
            CommandLine.INSTANCE.LogError(
                $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
            );
            return;
        }
        Vector3 camPos = PerspectiveRenderer.INSTANCE.CamPos;
        Quaternion camRot = PerspectiveRenderer.INSTANCE.CamRot;
        Vector3 nearFarFOV = PerspectiveRenderer.INSTANCE.NearFarFOV;
        Vector3 temp = new Vector3();
        Quaternion tempQuaternion = new Quaternion();
        if (@params[1].Split(',').Count() == 3)
        {
            if (
                (
                    !float.TryParse(@params[1].Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out temp.x)
                    && @params[1].Split(',')[0] != ""
                )
                || (
                    !float.TryParse(@params[1].Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out temp.y)
                    && @params[1].Split(',')[1] != ""
                )
                || (
                    !float.TryParse(@params[1].Split(',')[2], NumberStyles.Any, CultureInfo.InvariantCulture, out temp.z)
                    && @params[1].Split(',')[2] != ""
                )
            )
            {
                CommandLine.INSTANCE.LogError(
                    $"{@params[1]} has not been recognized as a valid coordinate."
                );
                return;
            }
            camPos.x = @params[1].Split(',')[0] == "" ? camPos.x : temp.x;
            camPos.y = @params[1].Split(',')[1] == "" ? camPos.y : temp.y;
            camPos.z = @params[1].Split(',')[2] == "" ? camPos.z : temp.z;
        }
        if (@params.Count() >= 3 && @params[2].Split(',').Count() == 3)
        {
            if (
                (
                    !float.TryParse(@params[2].Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out temp.x)
                    && @params[2].Split(',')[0] != ""
                )
                || (
                    !float.TryParse(@params[2].Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out temp.y)
                    && @params[2].Split(',')[1] != ""
                )
                || (
                    (
                        !float.TryParse(@params[2].Split(',')[2], NumberStyles.Any, CultureInfo.InvariantCulture, out temp.z)
                        || temp.z >= 180
                        || temp.z <= 0
                    )
                    && @params[2].Split(',')[2] != ""
                )
            )
            {
                CommandLine.INSTANCE.LogError(
                    $"{@params[2]} has not been recognized as a valid near,far,FOV set.\nnear and far clipping planes should have a strictly positive value and far should have a greater value than near.\nFOV should be between 0 and 180 exclusive."
                );
                return;
            }
            nearFarFOV.x = @params[2].Split(',')[0] == "" ? nearFarFOV.x : temp.x;
            nearFarFOV.y = @params[2].Split(',')[1] == "" ? nearFarFOV.y : temp.y;
            nearFarFOV.z = @params[2].Split(',')[2] == "" ? nearFarFOV.z : temp.z * Mathf.Deg2Rad;
        }
        if (@params.Count() >= 4 && @params[3].Split(',').Count() == 4)
        {
            if (
                (
                    !float.TryParse(@params[3].Split(',')[0], NumberStyles.Any, CultureInfo.InvariantCulture, out tempQuaternion.x)
                    && @params[3].Split(',')[0] != ""
                )
                || (
                    !float.TryParse(@params[3].Split(',')[1], NumberStyles.Any, CultureInfo.InvariantCulture, out tempQuaternion.y)
                    && @params[3].Split(',')[1] != ""
                )
                || (
                    !float.TryParse(@params[3].Split(',')[2], NumberStyles.Any, CultureInfo.InvariantCulture, out tempQuaternion.z)
                    && @params[3].Split(',')[2] != ""
                )
                || (
                    !float.TryParse(@params[3].Split(',')[3], NumberStyles.Any, CultureInfo.InvariantCulture, out tempQuaternion.w)
                    && @params[3].Split(',')[3] != ""
                )
            )
            {
                CommandLine.INSTANCE.LogError(
                    $"{@params[3]} has not been recognized as a valid quaternion."
                );
                return;
            }
            camRot.x = @params[3].Split(',')[0] == "" ? camRot.x : tempQuaternion.x;
            camRot.y = @params[3].Split(',')[1] == "" ? camRot.y : tempQuaternion.y;
            camRot.z = @params[3].Split(',')[2] == "" ? camRot.z : tempQuaternion.z;
            camRot.w = @params[3].Split(',')[3] == "" ? camRot.w : tempQuaternion.w;
        }
        Func<ProjectionMatrix> matrix = PerspectiveRenderer.INSTANCE.MatrixGen;
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("ISOMETRIC")) != default(string))
        {
            matrix = PerspectiveRenderer.INSTANCE.GetIsometricMatrix;
        }
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("ORTHOGRAPHIC")) != default(string))
        {
            matrix = PerspectiveRenderer.INSTANCE.GetOrthographicMatrix;
        }
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("PERPENDICULAR")) != default(string))
        {
            matrix = PerspectiveRenderer.INSTANCE.GetPerpendicularMatrix;
        }
        if (@params.FirstOrDefault((s) => s.ToUpper().Equals("PERSPECTIVE")) != default(string))
        {
            matrix = PerspectiveRenderer.INSTANCE.GetPerspectiveMatrix;
        }

        PerspectiveRenderer.INSTANCE.CamPos = camPos;
        PerspectiveRenderer.INSTANCE.CamRot = camRot.normalized;
        PerspectiveRenderer.INSTANCE.NearFarFOV = nearFarFOV;
        PerspectiveRenderer.INSTANCE.Matrix = matrix();
        PerspectiveRenderer.INSTANCE.MatrixGen = matrix;
    }

    public override string GetShortDescription()
    {
        return "Changes the options for the 3D-renderer.";
    }

    public override string GetSyntaxHelp()
    {
        return ">render3D [x,y,z] [near,far,FOV] [x,y,z,w] [ISOMETRIC | ORTHOGRAPHIC | PERPENDICULAR | PERSPECTIVE]\n    - x,y,z - Positional coordinates of the observing camera. If none are specified, the current coordinates are kept.\n    - near,far,FOV: Cameras near and far clipping planes and the camera FOV. If the rendering mode does not have depth, FOV is used as the left camera bound in world units. If none is specified, the current values are kept.\n    - x,y,z,w Quaternion rotation of the observing coordinate using a left handed system. If none is specified, the current quaternion is kept.\n    - ISOMETRIC | ORTHOGRAPIC | PERPENDICULAR | PERSPECTIVE: Select a rendering method, if none is specified, the current rendering method is kept.";
    }

    public override string GetName()
    {
        return "render3d";
    }
}

public class SaveCommand : Command
{
    public override void Execute(params string[] @params)
    {
        if (@params.Count() != 2)
        {
            CommandLine.INSTANCE.LogError(
                $"No overload for {GetName().ToUpper()} takes {@params.Count() - 1} argument{(@params.Count() == 2 ? "" : "s")}."
            );
            return;
        }
        try
        {
            if (CameraScript.INSTANCE.fourier.Rotators.Count() == 0)
            {
                CommandLine.INSTANCE.LogError($"No Fourier series is currently loaded.");
                return;
            }
            string[] lines = FourierGenerator.Save(CameraScript.INSTANCE.fourier);
            File.WriteAllLines(@params[1], lines);
        }
        catch
        {
            CommandLine.INSTANCE.LogError($"{@params[1]} is not a valid path.");
        }
    }

    public override string GetShortDescription()
    {
        return "Saves the current Fourier series to a file.";
    }

    public override string GetSyntaxHelp()
    {
        return ">save <path>\n    - path: absolute path of the file to save to.";
    }

    public override string GetName()
    {
        return "save";
    }
}
