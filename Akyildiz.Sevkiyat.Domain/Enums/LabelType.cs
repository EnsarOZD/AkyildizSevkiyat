namespace Akyildiz.Sevkiyat.Domain.Enums
{
    public enum LabelType
    {
        CargoLabel = 0,
        BoxLabel   = 1,
    }

    public enum PrintJobStatus
    {
        Pending  = 0,
        Printing = 1,
        Done     = 2,
        Failed   = 3,
    }
}
