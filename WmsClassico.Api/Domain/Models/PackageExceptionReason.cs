namespace WmsClassico.Api.Domain.Models;

public enum PackageExceptionReason
{
    Nenhum = 0,
    EnvioCancelado = 1,
    Avariado = 2,
    RotaInvalida = 3,
    FluxoIncoerente = 4,
    DestinoNaoMapeado = 5,
    CapacidadeBloqueada = 6,
    AguardandoIrmao = 7,
    AguardandoDataDeSaida = 8,
    SemRecebimentoNoSistema = 9,
    DivergenciaDeBipagem = 10
}
