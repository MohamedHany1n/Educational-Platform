
namespace EducationalPlatform.Web.Models;

public class AttemptAnswerOption
{
    public int Id { get; set; }

    public int AttemptAnswerId { get; set; }
    public AttemptAnswer AttemptAnswer { get; set; } = null!;

    public int QuestionOptionId { get; set; }
    public QuestionOption QuestionOption { get; set; } = null!;

}
