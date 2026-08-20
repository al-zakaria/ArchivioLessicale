using System.Text.Json.Serialization;

namespace ArchivioLessicale.API.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserGrade
{
    A1,
    A2,
    B1,
    B2,
    C1,
    C2

}
