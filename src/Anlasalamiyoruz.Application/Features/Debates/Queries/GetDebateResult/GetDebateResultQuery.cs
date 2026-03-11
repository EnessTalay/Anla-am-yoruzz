using MediatR;

namespace Anlasalamiyoruz.Application.Features.Debates.Queries.GetDebateResult;

public record GetDebateResultQuery(Guid DebateId) : IRequest<AnalysisResultDto>;
