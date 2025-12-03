// SunSkog.Api/Storage/Entities/StockMovementType.cs
namespace SunSkog.Api.Storage.Entities;

public enum StockMovementType
{
    In = 1,          // příjem na sklad
    Out = 2,         // výdej ze skladu
    Adjustment = 3,  // korekce stavu
    Assignment = 4,  // přidělení zaměstnanci
    Return = 5       // vrácení od zaměstnance
}