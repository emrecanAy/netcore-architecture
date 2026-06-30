using FluentValidation;

namespace OrderSystem.Queries.HelpDesk.AskQuestion;

public class AskQuestionQueryValidator : AbstractValidator<AskQuestionQuery>
{
    public AskQuestionQueryValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TopK).GreaterThan(0).When(x => x.TopK.HasValue);
    }
}
