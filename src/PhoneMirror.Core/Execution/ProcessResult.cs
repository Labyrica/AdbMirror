namespace PhoneMirror.Core.Execution;

/// <summary>
/// Represents the result of an external process execution.
/// </summary>
/// <param name="ExitCode">The exit code of the process. 0 typically indicates success.</param>
/// <param name="StandardOutput">The captured standard output of the process.</param>
/// <param name="StandardError">The captured standard error of the process.</param>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>
    /// Returns true if the process exited with code 0.
    /// </summary>
    public bool Success => ExitCode == 0;
}
