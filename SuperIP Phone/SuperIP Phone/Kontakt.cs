namespace SuperIP_Phone
{
    class Kontakt
    {
        public string login;
        public string imie;
        public string nazwisko;
        public string dzial;
        public string AdresIP;

        public Kontakt(string login, string imie, string nazwisko, string dzial, string AdresIP)
        {
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
            if (AdresIP != "")
            {
                return login + "\n" + imie + " " + nazwisko + "\n" + dzial + "\n" + AdresIP;
            }
            else
            {
                return login + "\n" + imie + " " + nazwisko + "\n" + dzial;
            }
        }
    }
}
