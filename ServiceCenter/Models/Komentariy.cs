using System;
using System.Windows.Media.Imaging;

namespace ServiceCenter.Models
{
    public class Komentariy
    {
        public int KomentariyId { get; set; }
        public int ZayavkaId { get; set; }
        public int PolzovatelId { get; set; }
        public string ImyaPolzovatelya { get; set; }
        public string TekstKomentariya { get; set; }
        public DateTime DataSozdaniya { get; set; }
        public bool MozhnoUdolit { get; set; }
        public bool ImeetQRCode { get; set; }
        public BitmapImage QRCodeImage { get; set; }
        public string SsylkaFormi { get; set; }
        public string TekstDlyaOtobrazheniya { get; set; }
    }
}