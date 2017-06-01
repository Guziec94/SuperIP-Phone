using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace SuperIP_Phone
{
    public class baza_danych
    {
        public static SqlConnection cnn;
        public static string connectionString = "Data Source=tcp:LenovoY510P;Initial Catalog = SIPPhone; User ID = Guziec94; Password=P@ssw0rd; MultipleActiveResultSets = True;";
        public static void polacz_z_baza()
        {
            cnn = new SqlConnection(connectionString);
            try
            {
                cnn.Open();
            }
            catch (Exception)
            {
                MessageBox.Show("Nie można nawiązać połączenia!");
            }
        }

        static SqlDependency dependency;
        public async static void broker()
        {
            SqlDependency.Start(connectionString);
            var _connection = new SqlConnection(connectionString);
            _connection.Open();
            SqlCommand _sqlCommand = new SqlCommand("SELECT przeladuj_kontakty FROM dbo.lista_zdarzen where login = @login", _connection);
            _sqlCommand.Parameters.AddWithValue("login", ((Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"]).login);
            _sqlCommand.Notification = null;
            dependency = new SqlDependency(_sqlCommand);
            dependency.OnChange += SqlDependencyOnChange;
            await _sqlCommand.ExecuteReaderAsync();
        }
        
        private static void SqlDependencyOnChange(object sender, SqlNotificationEventArgs eventArgs)
        {
            if (eventArgs.Info == SqlNotificationInfo.Update)
            {
                string query = "SELECT [przeladuj_kontakty] FROM dbo.lista_zdarzen where login = @login";
                SqlCommand executeQuery = new SqlCommand(query, cnn);
                executeQuery.Parameters.AddWithValue("login", ((Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"]).login);
                using (executeQuery)
                {
                    using (SqlDataReader readerQuery = executeQuery.ExecuteReader())
                    {
                        if (readerQuery.Read())
                        {
                            bool przeladowac_kontakty = readerQuery.GetSqlBoolean(0) == true ? true : false;
                            readerQuery.Close();
                            if (przeladowac_kontakty)//ktos zniknal z listy kontaktow lub zmiana statusu dostepnosci z listy kontaktow
                            {
                                ((StronaGlowna)System.Windows.Application.Current.Properties["strona_glowna"]).OdswiezListeKontaktow();
                                query = "update lista_zdarzen set przeladuj_kontakty=0 where login = @login";//wyzerowanie eventu
                                SqlCommand updateQuery = new SqlCommand(query, cnn);
                                updateQuery.Parameters.AddWithValue("login", ((Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"]).login);
                                updateQuery.ExecuteNonQuery();
                            }
                        }
                    }
                }
                broker();
            }
        }
        
        public static void broker_stop()
        {
            SqlDependency.Stop(connectionString);
            dependency.OnChange -= SqlDependencyOnChange;
            dependency = null;
        }
        
        public static Dictionary<int, string> pobierz_liste_dzialow()
        {
            Dictionary<int, string> lista_dzialow = new Dictionary<int, string>();
            string zapytanie = "select ID_dzialu, nazwa_dzialu from dzialy";
            SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
            using (executeQuery)
            {
                try
                {
                    using (SqlDataReader readerQuery = executeQuery.ExecuteReader())
                    {
                        while (readerQuery.Read())
                        {
                            lista_dzialow.Add(readerQuery.GetInt32(0),readerQuery.GetString(1));
                        }
                        readerQuery.Close();
                        if (lista_dzialow.Count > 0)
                        {
                            return lista_dzialow;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wystąpił nieoczekiwany błąd!\n" + ex.Message);
                    return null;
                }
            }
        }

        public static string utworz_Sha256(string text)
        {
            byte[] bytes = Encoding.Default.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += string.Format("{0:x2}", x);
            }
            return hashString;
        }

        public static bool zarejestruj(string login, string haslo, string imie, string nazwisko, int id_dzialu)
        {
            try
            {
                string zapytanie = "INSERT INTO uzytkownicy (login,skrot_hasla,imie,nazwisko,id_dzialu, adres_IP) VALUES (@login, @skrot_hasla, @imie, @nazwisko, @ID_dzialu, @adres_IP); Insert into lista_zdarzen (login) values (@login)";
                string skrot_hasla = utworz_Sha256(haslo);
                SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
                executeQuery.Parameters.AddWithValue("login", login);
                executeQuery.Parameters.AddWithValue("skrot_hasla", skrot_hasla);
                executeQuery.Parameters.AddWithValue("imie", imie);
                executeQuery.Parameters.AddWithValue("nazwisko", nazwisko);
                executeQuery.Parameters.AddWithValue("id_dzialu", id_dzialu);
                executeQuery.Parameters.AddWithValue("adres_IP", " ");
                executeQuery.ExecuteNonQuery();
                MessageBox.Show("Konto zarejestrowane, teraz możesz się zalogować.","Sukces",MessageBoxButton.OK,MessageBoxImage.Information);
                return true;
            }
            catch(SqlException ex)
            {
                if (ex.Class == 14 && ex.Number==2627)
                {
                    MessageBox.Show("Konto nie zostało zarejestrowane, podany login jest już używany przez innego użytkownika.", "Podany login jest zajęty.", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Wystąpił nieoczekiwany błąd!\n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return false;
            }
        }

        public static bool zaloguj(string login, string haslo)
        {
            string skrot_hasla = utworz_Sha256(haslo);
            string zapytanie = "select skrot_hasla, imie, nazwisko, d.nazwa_dzialu from uzytkownicy u join dzialy d on d.ID_dzialu = u.ID_dzialu where login = @login";
            try
            {
                SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
                executeQuery.Parameters.AddWithValue("login", login);
                using (executeQuery)
                {
                    using (SqlDataReader readerQuery = executeQuery.ExecuteReader())
                    {
                        while (readerQuery.Read())
                        {
                            if (skrot_hasla == readerQuery.GetString(0))
                            {
                                Kontakt zalogowany = new Kontakt(login, readerQuery.GetString(1), readerQuery.GetString(2), readerQuery.GetString(3), System.Windows.Application.Current.Properties["AdresIP"].ToString());
                                System.Windows.Application.Current.Properties["ZalogowanyUzytkownik"] = zalogowany;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił nieoczekiwany błąd!\n" + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return false;
        }

        public static void ustaw_status(bool czy_zalogowany)//Funkcja ustawiająca status użytkownika.
        { 
            string zapytanie;
            SqlCommand executeQuery;
            if (czy_zalogowany)//uzytkownik wlasnie sie zalogowal - ustawiamy status na dostepny wpisujac do bazy jego IP
            {
                zapytanie = "Update uzytkownicy set adres_IP = @AdresIP where login = @login;update lista_zdarzen set przeladuj_kontakty=1 where login in (select login from uzytkownicy where login in (SELECT Value FROM STRING_SPLIT((select lista_kontaktow from uzytkownicy where login = @login), ',')) and adres_IP != ' ')";
                executeQuery = new SqlCommand(zapytanie, cnn);
                executeQuery.Parameters.AddWithValue("AdresIP", System.Windows.Application.Current.Properties["AdresIP"]);
            }
            else//uzytkownik sie wylogowal/zamknął aplikację - usuwamy jego adres z bazy danych
            {
                zapytanie = "Update uzytkownicy set adres_IP = ' ' where login = @login; update lista_zdarzen set przeladuj_kontakty=1 where login in (select login from uzytkownicy where login in (SELECT Value FROM STRING_SPLIT((select lista_kontaktow from uzytkownicy where login = @login), ',')) and adres_IP != ' ')";
                executeQuery = new SqlCommand(zapytanie, cnn);
            }
            executeQuery.Parameters.AddWithValue("login", ((Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"]).login);
            executeQuery.ExecuteNonQuery();
        }
        
        public static void usun_konto()
        {
            string zapytanie = "delete from uzytkownicy where login=@login; delete from lista_zdarzen where login=@login; delete from lista_oczekujacych where login_dodajacego = @login or login_dodawanego = @login;";
            SqlCommand deleteQuery = new SqlCommand(zapytanie, cnn);
            deleteQuery.Parameters.AddWithValue("login", ((Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"]).login);
            deleteQuery.ExecuteNonQuery();
        }

        internal static List<Kontakt> pobierz_liste_kontaktow()
        {
            string zapytanie = "select login, imie, nazwisko, d.nazwa_dzialu, adres_IP from uzytkownicy u join dzialy d on d.ID_dzialu = u.ID_dzialu where login in (SELECT Value FROM STRING_SPLIT((select lista_kontaktow from uzytkownicy where login = @login), ','))";
            SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
            executeQuery.Parameters.AddWithValue("login", ((Kontakt)Application.Current.Properties["ZalogowanyUzytkownik"]).login);
            List<Kontakt> lista_kontaktow = new List<Kontakt>();
            using (executeQuery)
            {
                try
                {
                    using (SqlDataReader readerQuery = executeQuery.ExecuteReader())
                    {
                        while (readerQuery.Read())
                        {
                            lista_kontaktow.Add(new Kontakt(readerQuery.GetString(0), readerQuery.GetString(1), readerQuery.GetString(2), readerQuery.GetString(3), readerQuery.GetString(4)));
                        }
                        readerQuery.Close();
                        if (lista_kontaktow.Count > 0)
                        {
                            return lista_kontaktow;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wystąpił nieoczekiwany błąd!\n" + ex.Message);
                    return null;
                }
            }
        }

        internal static List<Kontakt> wyszukaj_kontakty(string _login, string _imie, string _nazwisko, string _ID_dzialu)
        {
            string zapytanie = "select login, imie, nazwisko, d.nazwa_dzialu, adres_IP from uzytkownicy u join dzialy d on d.ID_dzialu = u.ID_dzialu  where login like @login and imie like @imie and nazwisko like @nazwisko and u.ID_dzialu like @ID_dzialu";
            SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
            executeQuery.Parameters.AddWithValue("login", "%"+_login+"%");
            executeQuery.Parameters.AddWithValue("imie", _imie);
            executeQuery.Parameters.AddWithValue("nazwisko", _nazwisko);
            executeQuery.Parameters.AddWithValue("ID_dzialu", _ID_dzialu);
            List<Kontakt> znalezione_kontakty = new List<Kontakt>();
            using (executeQuery)
            {
                try
                {
                    using (SqlDataReader readerQuery = executeQuery.ExecuteReader())
                    {
                        while (readerQuery.Read())
                        {
                            znalezione_kontakty.Add(new Kontakt(readerQuery.GetString(0), readerQuery.GetString(1), readerQuery.GetString(2), readerQuery.GetString(3),""));
                        }
                        readerQuery.Close();
                        if (znalezione_kontakty.Count > 0)
                        {
                            znalezione_kontakty.Sort((x, y) => string.Compare(x.nazwisko+x.imie, y.nazwisko+y.imie));
                            return znalezione_kontakty;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Wystąpił nieoczekiwany błąd!\n" + ex.Message);
                    return null;
                }
            }
        }

        public static void dodaj_uzytkownika_do_kontaktow(string login_dodajacego, string login_dodawanego)//funkcja dopisuje do listy kontaktów użytkownika login_dodajacego nowy kontakt, którym jest login_dodawanego
        {
            string zapytanie = "update uzytkownicy set lista_kontaktow=Concat((select top(1) lista_kontaktow FROM uzytkownicy where login=@login_dodajacego), @login_dodawanego) where login=@login_dodajacego";
            SqlCommand query = new SqlCommand(zapytanie, cnn);
            query.Parameters.AddWithValue("login_dodajacego", login_dodajacego);
            query.Parameters.AddWithValue("login_dodawanego", ","+ login_dodawanego);//login_dodawanego jest wstawiany na końcu listy, dlatego jest on poprzedzony przecinkiem
            query.ExecuteNonQuery();
        }

        public static void usun_uzytkownika_z_kontaktow(string login_usuwajacego, string login_usuwanego)//funkcja dopisuje do listy kontaktów użytkownika login_dodajacego nowy kontakt, którym jest login_dodawanego
        {
            string zapytanie = "DECLARE @Logins VARCHAR(MAX);SELECT @Logins = COALESCE(@Logins + ',', '') +  value from string_split((select top(1) lista_kontaktow FROM uzytkownicy where login = @login_usuwajacego), ',') where value != @login_usuwanego; Update uzytkownicy set lista_kontaktow = @Logins where login=@login_usuwajacego;";
            SqlCommand query = new SqlCommand(zapytanie, cnn);
            query.Parameters.AddWithValue("login_usuwajacego", login_usuwajacego);
            query.Parameters.AddWithValue("login_usuwanego", login_usuwanego);
            query.ExecuteNonQuery();
        }
    }
}
