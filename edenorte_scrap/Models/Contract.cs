using Postgrest.Attributes;
using Supabase;

namespace edenorte_scrap.Models
{
    [Table("contract")]

    public class Contract: SupabaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Column("number")]

        public string Number { get; set; }
        /// <summary>
        /// 
        /// </summary>
        [Column("detail_url")]

        public string DetailUrl { get; set; }
    }
}
