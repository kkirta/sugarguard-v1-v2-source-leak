namespace SugarGuard.Protector.Enums
{
    public enum Protections
    {
        Constants = 0,
        PosConstants = 1,
        VM = 2,
        ReferenceProxy = 3,
        ControlFlow = 4,
        InvalidOpcodes = 5,
        Renamer = 6,
        AntiDebug = 7,
        AntiDump = 8,
        ReferenceOverload = 9,
        WaterMark = 10,
        CallConvertion = 11,
        FakeAttributes = 12,
        MethodHider = 13,
        Mutation = 14,
    }
}