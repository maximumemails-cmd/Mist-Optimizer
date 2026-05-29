namespace PCOptimizer.Models;

public sealed record SwuabApplyResult(
    bool Succeeded,
    bool RequiresAdmin,
    bool IsSupported,
    string Message,
    string CommandText);
