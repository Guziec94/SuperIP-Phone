using System.Collections.Generic;

namespace SuperIP_Phone
{
    class Kontakt
    {
        public string login;
        public string imie;
        public string nazwisko;
        public string dzial;
        public string AdresIP;
        public List<string> wiadomosci;

        public Kontakt(string login, string imie, string nazwisko, string dzial, string AdresIP)
        {
            wiadomosci = new List<string>();
            this.login = login;
            this.imie = imie;
            this.nazwisko = nazwisko;
            this.dzial = dzial;
            if (AdresIP == " ")
            {
                this.AdresIP = "";
            }
            else
            {
                this.AdresIP = AdresIP;
            }
        }

        public override string ToString()
        {
            return string.Format("Login: {0}\n{1} {2}\nDział: {3}", login, imie, nazwisko, dzial); //login + "\n" + nazwisko + " " + imie + "\n" + dzial + "\n" + AdresIP;
        }
    }
}
