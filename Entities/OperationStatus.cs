namespace Entities
{
    public enum OperationStatus
    {
        OK = 1,
        Error = 2,
        LabelNotLoaded = 3,
        ProductNotFound = 4,
        LabelNotPicked = 5,
        LabelNotApplied = 6,
        LabelAlreadyPrinted = 7,
        LabelNotPrinted = 8,
        Cycling = 9,
        RobotMovementFailed = 10,
        ProductNotLoaded = 11,
        IncongruentOperativeMode = 12,
        RobotPositionError = 13,
    }
}
