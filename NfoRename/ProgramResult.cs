namespace NfoRename;

internal record ProgramResult(int Result, int CheckedFiles, int Errors)
{
    public const int Ok = 0;
    public const int FileCheckFailed = 1;
    public const int FileRepairFailed = 2;
}