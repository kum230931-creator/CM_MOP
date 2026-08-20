namespace CMOmeets.Application.Masters;

// Thrown when an officer save would assign one or more posts (designations) already held by other
// active officers and the caller has not confirmed the reassignment (Force). The controller turns
// this into a 409 with the conflict list so the UI can prompt "remove & reassign".
public class DesignationConflictException : Exception
{
    public IReadOnlyList<DesignationConflictDto> Conflicts { get; }
    public DesignationConflictException(IReadOnlyList<DesignationConflictDto> conflicts)
        : base("One or more posts are already assigned to another officer.")
        => Conflicts = conflicts;
}
