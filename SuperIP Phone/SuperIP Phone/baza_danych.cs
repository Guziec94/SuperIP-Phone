using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SuperIP_Phone
{
    public class baza_danych
    {
        public static SqlConnection cnn;
        public static string connectionString = "Data Source=tcp:LENOVOY510P;Initial Catalog = SIPPhone; User ID = Guziec94; Password=P@ssw0rd";
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

        public static void rozlacz_z_baza()
        {
            try
            {
                cnn.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("Nie udało się rozłączyć.");
            }
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
                string zapytanie = "INSERT INTO uzytkownicy (login,skrot_hasla,imie,nazwisko,id_dzialu,lista_kontaktow) VALUES (@login, @skrot_hasla, @imie, @nazwisko, @id_dzialu,@lista_kontaktow)";
                string skrot_hasla = utworz_Sha256(haslo);
                SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
                executeQuery.Parameters.AddWithValue("login", login);
                executeQuery.Parameters.AddWithValue("skrot_hasla", skrot_hasla);
                executeQuery.Parameters.AddWithValue("imie", imie);
                executeQuery.Parameters.AddWithValue("nazwisko", nazwisko);
                executeQuery.Parameters.AddWithValue("id_dzialu", id_dzialu);
                executeQuery.Parameters.AddWithValue("lista_kontaktow", "<lista_kontaktow></lista_kontaktow>");
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
            string zapytanie = "select skrot_hasla from uzytkownicy where login = @login";
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

        public static void wprowadz_adres_IP()
        {
            string zapytanie = "Update uzytkownicy set AdresIP = @AdresIP where login = @login";
            SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
            executeQuery.Parameters.AddWithValue("login", Application.Current.Properties["Login"]);
            executeQuery.Parameters.AddWithValue("AdresIP", Application.Current.Properties["AdresIP"]);
            executeQuery.ExecuteNonQuery();
        }

        public static void usun_adres_IP()
        {
            string zapytanie = "Update uzytkownicy set AdresIP = NULL where login = @login";
            SqlCommand executeQuery = new SqlCommand(zapytanie, cnn);
            executeQuery.Parameters.AddWithValue("login", Application.Current.Properties["Login"]);
            executeQuery.ExecuteNonQuery();
        }
    }
}
