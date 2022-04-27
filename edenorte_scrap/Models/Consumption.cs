using Postgrest.Attributes;
using Supabase;

namespace edenorte_scrap.Models
{
    [Table("consumption")]
    public class Consumption : SupabaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("reading_delivered")]

        public double ReadingDelivered { get; set; }
        [Column("current_measure")]

        public double CurrentMeasure { get; set; }
        [Column("data_available_up_to")]

        public DateTime DataAvailableUpTo { get; set; }
        [Column("last_invoice")]

        public DateTime LastInvoice { get; set; }

        [Column("consumption_till_now")]

        public double ConsumptionTillNow { get; set; }
        [Column("projected_consumption")]

        public double ProjectedConsumption { get; set; }
        [Column("max_consumption_date")]

        public DateTime? MaxConsumptionDate { get; set; }
        [Column("consumption_value")]

        public double ConsumptionValue { get; set; }
        [Column("contract_id")]

        public int ContractId { get; set; }

        [Column("created_at")]

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;


    }
}
