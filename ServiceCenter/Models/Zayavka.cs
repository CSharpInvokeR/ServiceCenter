using System;

namespace ServiceCenter.Models
{
    public class Zayavka
    {
        public int ZayavkaId { get; set; }
        public string NomerZayavki { get; set; }
        public DateTime DataSozdaniya { get; set; }
        public int OborudovanieId { get; set; }
        public string NazvanieOborudovaniya { get; set; }
        public int TipNeispravnostiId { get; set; }
        public string NazvanieTipa { get; set; }
        public string Opisanie { get; set; }
        public string Status { get; set; }
        public int KlientId { get; set; }
        public string ImyaKlienta { get; set; }
        public int? NaznachenoKomu { get; set; }
        public string ImyaIspolnitelya { get; set; }
        public int Sozdal { get; set; }
        public string ImyaSozdavshego { get; set; }
    }
}