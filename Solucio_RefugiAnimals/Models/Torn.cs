namespace Solucio_RefugiAnimals.Models;

// Com l'Especie: a la BD seria un enter (MATI = 0...). Amb HasConversion<string>() al context surt el text
public enum Torn
{
    MATI,
    TARDA,
    CAP_DE_SETMANA
}
