using Ardalis.SmartEnum;

namespace edenorte_scrap.Models
{
    public sealed class Item : SmartEnum<Item>
    {
        public static readonly Item Meter = new("Medidor", 1);
        public static readonly Item Bidirectional = new("Bidireccional", 2);
        public static readonly Item Rate = new("Tarifa", 3);
        public static readonly Item MeterChange = new("Fecha cambio medidor", 4);
        public static readonly Item Multiple = new("M&uacute;ltiplo actual", 5);
        public static readonly Item LastInvoice = new("Fecha &uacute;ltima Factura", 6);
        public static readonly Item DataUntil = new("Datos disponibles hasta el d&iacute;a", 7);
        public static readonly Item ConsumptionUntilNow = new("Consumo hasta la fecha (kWh)", 8);
        public static readonly Item ConsumptionProjection = new("Proyecci&oacute;n de consumo (kWh)", 9);
        public static readonly Item MaxConsumptionDate = new("D&iacute;a de mayor consumo", 10);
        public static readonly Item CurrentConsumption = new("Valor de consumo (kWh)", 11);
        public static readonly Item CurrentReading = new("Medidio actual", 12);

        private Item(string name, ushort value) : base(name, value)
        {
        }
    }
}
