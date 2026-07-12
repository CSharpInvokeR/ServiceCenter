using System;

namespace ServiceCenter.Models
{
    public class Polzovatel
    {
        public int PolzovatelId { get; set; }
        public string PolnoeImya { get; set; }
        public string Login { get; set; }
        public string Parol { get; set; }
        public string Rol { get; set; }
        public string Telefon { get; set; }
        public string Email { get; set; }
    }
}