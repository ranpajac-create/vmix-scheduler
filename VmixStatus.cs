namespace VmixScheduler;

public class VmixStatus
{
    public List<VmixInput> Inputs { get; set; } = new();
    public string ActiveNumber { get; set; } = "";

    public VmixInput? FindActive() => Inputs.FirstOrDefault(i => i.Number == ActiveNumber);
}
