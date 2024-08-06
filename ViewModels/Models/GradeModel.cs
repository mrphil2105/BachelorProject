namespace Apachi.ViewModels.Models;

public record GradeModel(int Value, string GradeName)
{
    public static IReadOnlyList<GradeModel> ValidGrades { get; } =
        new List<GradeModel>
        {
            new GradeModel(-3, "-3"),
            new GradeModel(0, "00"),
            new GradeModel(2, "02"),
            new GradeModel(4, "4"),
            new GradeModel(7, "7"),
            new GradeModel(10, "10"),
            new GradeModel(12, "12")
        };
}
