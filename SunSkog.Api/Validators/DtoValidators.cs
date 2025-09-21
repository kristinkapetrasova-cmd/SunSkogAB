using System.Linq;
using FluentValidation;
using SunSkog.Api.Contracts;

namespace SunSkog.Api.Validators
{
    // --- AUTH ---
    public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);

            var allowed = new[] { "Employee", "Manager", "Admin", "SuperAdmin" };
            RuleFor(x => x.Role)
                .NotEmpty()
                .Must(r => allowed.Contains(r ?? string.Empty))
                .WithMessage("Role musí být jedna z: Employee, Manager, Admin, SuperAdmin.");
        }
    }

    public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
    {
        public LoginDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    // --- TIMESHEETS ---
    public sealed class NoteDtoValidator : AbstractValidator<NoteDto>
    {
        public NoteDtoValidator()
        {
            RuleFor(x => x.Notes).MaximumLength(1000);
        }
    }

    public sealed class EntryDtoValidator : AbstractValidator<EntryDto>
    {
        public EntryDtoValidator()
        {
            RuleFor(x => x.WorkDate).NotEmpty();
            RuleFor(x => x.Hours).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Km).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Pieces).GreaterThanOrEqualTo(0);
            RuleFor(x => x.HourRate).GreaterThanOrEqualTo(0);
            RuleFor(x => x.KmRate).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PieceRate).GreaterThanOrEqualTo(0);

            RuleFor(x => x)
                .Must(e => (e.Hours > 0) || (e.Km > 0) || (e.Pieces > 0))
                .WithMessage("Musí být vyplněny alespoň Hours, Km nebo Pieces (nenulové).");
        }
    }

    public sealed class CreateDtoValidator : AbstractValidator<CreateDto>
    {
        public CreateDtoValidator()
        {
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).NotEmpty();

            RuleFor(x => x)
                .Must(x => x.PeriodStart <= x.PeriodEnd)
                .WithMessage("periodStart musí být menší nebo rovno periodEnd.");

            When(x => x.Entries != null && x.Entries.Any(), () =>
            {
                RuleForEach(x => x.Entries!).SetValidator(new EntryDtoValidator());

                RuleFor(x => x).Custom((dto, ctx) =>
                {
                    for (int i = 0; i < dto.Entries!.Count; i++)
                    {
                        var e = dto.Entries[i];
                        if (e.WorkDate < dto.PeriodStart || e.WorkDate > dto.PeriodEnd)
                        {
                            ctx.AddFailure($"entries[{i}].workDate",
                                "workDate musí být v intervalu periodStart..periodEnd.");
                        }
                    }
                });
            });
        }
    }
}