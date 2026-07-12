using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using ServiceCenter.Models;
using QRCoder;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace ServiceCenter.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = ConfigHelper.GetConnectionString();
        }

        public string GenerirovatQRCode(string text)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                    return Convert.ToBase64String(qrCodeBytes);
                }
            }
        }

        public async Task<bool> DobavitQRCodeKomentariy(int zayavkaId, int polzovatelId)
        {
            try
            {
                string ssylka = ConfigHelper.GetFeedbackFormUrl();

                var komentariy = new Komentariy
                {
                    ZayavkaId = zayavkaId,
                    PolzovatelId = polzovatelId,
                };

                return await DobavitKomentariy(komentariy);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка: " + ex.Message);
            }
        }

        public async Task<Polzovatel> AvtorizovatPolzovatelya(string login, string parol)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Polzovateli WHERE Login = @Login AND Parol = @Parol";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Parol", parol);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Polzovatel
                            {
                                PolzovatelId = reader.GetInt32(0),
                                PolnoeImya = reader.GetString(1),
                                Login = reader.GetString(2),
                                Parol = reader.GetString(3),
                                Rol = reader.GetString(4),
                                Telefon = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Email = reader.IsDBNull(6) ? "" : reader.GetString(6)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> ZaregistrirovatKlienta(string fullName, string login, string password, string phone, string email)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string checkQuery = "SELECT COUNT(*) FROM Polzovateli WHERE Login = @Login";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@Login", login);
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        throw new Exception("Пользователь с таким логином уже существует");
                    }
                }

                string query = @"
            INSERT INTO Polzovateli (PolnoeImya, Login, Parol, Rol, Telefon, Email) 
            VALUES (@PolnoeImya, @Login, @Parol, 'Клиент', @Telefon, @Email);
            SELECT SCOPE_IDENTITY();";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PolnoeImya", fullName);
                    command.Parameters.AddWithValue("@Login", login);
                    command.Parameters.AddWithValue("@Parol", password);
                    command.Parameters.AddWithValue("@Telefon", string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone);
                    command.Parameters.AddWithValue("@Email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);

                    object result = await command.ExecuteScalarAsync();
                    return result != null && result != DBNull.Value;
                }
            }
        }

        public async Task<List<Zayavka>> PoluchitVseZayavki()
        {
            List<Zayavka> zayavki = new List<Zayavka>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT 
                        z.ZayavkaId, z.NomerZayavki, z.DataSozdaniya, 
                        z.OborudovanieId, o.Nazvanie as NazvanieOborudovaniya,
                        z.TipNeispravnostiId, t.NazvanieTipa,
                        z.Opisanie, z.Status,
                        z.KlientId, k.PolnoeImya as ImyaKlienta,
                        z.NaznachenoKomu, i.PolnoeImya as ImyaIspolnitelya,
                        z.Sozdal, s.PolnoeImya as ImyaSozdavshego
                    FROM Zayavki z
                    LEFT JOIN Oborudovanie o ON z.OborudovanieId = o.OborudovanieId
                    LEFT JOIN TipyNeispravnostey t ON z.TipNeispravnostiId = t.TipNeispravnostiId
                    LEFT JOIN Polzovateli k ON z.KlientId = k.PolzovatelId
                    LEFT JOIN Polzovateli i ON z.NaznachenoKomu = i.PolzovatelId
                    LEFT JOIN Polzovateli s ON z.Sozdal = s.PolzovatelId
                    ORDER BY z.DataSozdaniya DESC";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        zayavki.Add(new Zayavka
                        {
                            ZayavkaId = reader.GetInt32(0),
                            NomerZayavki = reader.GetString(1),
                            DataSozdaniya = reader.GetDateTime(2),
                            OborudovanieId = reader.GetInt32(3),
                            NazvanieOborudovaniya = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            TipNeispravnostiId = reader.GetInt32(5),
                            NazvanieTipa = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            Opisanie = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            Status = reader.GetString(8),
                            KlientId = reader.GetInt32(9),
                            ImyaKlienta = reader.IsDBNull(10) ? "" : reader.GetString(10),
                            NaznachenoKomu = reader.IsDBNull(11) ? (int?)null : reader.GetInt32(11),
                            ImyaIspolnitelya = reader.IsDBNull(12) ? "" : reader.GetString(12),
                            Sozdal = reader.GetInt32(13),
                            ImyaSozdavshego = reader.IsDBNull(14) ? "" : reader.GetString(14)
                        });
                    }
                }
            }

            return zayavki;
        }

        public async Task<List<Oborudovanie>> PoluchitVseOborudovanie()
        {
            List<Oborudovanie> list = new List<Oborudovanie>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Oborudovanie ORDER BY Nazvanie";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Oborudovanie
                        {
                            OborudovanieId = reader.GetInt32(0),
                            Nazvanie = reader.GetString(1),
                            Tip = reader.IsDBNull(2) ? "" : reader.GetString(2)
                        });
                    }
                }
            }

            return list;
        }

        public async Task<List<TipNeispravnosti>> PoluchitVseTipyNeispravnostey()
        {
            List<TipNeispravnosti> list = new List<TipNeispravnosti>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM TipyNeispravnostey ORDER BY NazvanieTipa";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new TipNeispravnosti
                        {
                            TipNeispravnostiId = reader.GetInt32(0),
                            NazvanieTipa = reader.GetString(1)
                        });
                    }
                }
            }

            return list;
        }

        public async Task<List<Polzovatel>> PoluchitVsehSotrudnikov()
        {
            List<Polzovatel> list = new List<Polzovatel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT PolzovatelId, PolnoeImya FROM Polzovateli WHERE Rol = 'Сотрудник' ORDER BY PolnoeImya";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Polzovatel
                        {
                            PolzovatelId = reader.GetInt32(0),
                            PolnoeImya = reader.GetString(1)
                        });
                    }
                }
            }

            return list;
        }

        public async Task<List<Polzovatel>> PoluchitVsehKlientov()
        {
            List<Polzovatel> list = new List<Polzovatel>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT PolzovatelId, PolnoeImya FROM Polzovateli WHERE Rol = 'Клиент' ORDER BY PolnoeImya";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        list.Add(new Polzovatel
                        {
                            PolzovatelId = reader.GetInt32(0),
                            PolnoeImya = reader.GetString(1)
                        });
                    }
                }
            }

            return list;
        }

        public async Task<bool> DobavitZayavku(Zayavka zayavka)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO Zayavki 
                    (NomerZayavki, DataSozdaniya, OborudovanieId, TipNeispravnostiId, Opisanie, Status, KlientId, NaznachenoKomu, Sozdal) 
                    VALUES 
                    (@NomerZayavki, @DataSozdaniya, @OborudovanieId, @TipNeispravnostiId, @Opisanie, @Status, @KlientId, @NaznachenoKomu, @Sozdal)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NomerZayavki", zayavka.NomerZayavki);
                    command.Parameters.AddWithValue("@DataSozdaniya", zayavka.DataSozdaniya);
                    command.Parameters.AddWithValue("@OborudovanieId", zayavka.OborudovanieId);
                    command.Parameters.AddWithValue("@TipNeispravnostiId", zayavka.TipNeispravnostiId);
                    command.Parameters.AddWithValue("@Opisanie", zayavka.Opisanie);
                    command.Parameters.AddWithValue("@Status", "В ожидании");
                    command.Parameters.AddWithValue("@KlientId", zayavka.KlientId);
                    command.Parameters.AddWithValue("@NaznachenoKomu", zayavka.NaznachenoKomu.HasValue ? (object)zayavka.NaznachenoKomu.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@Sozdal", zayavka.Sozdal);

                    int result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public async Task<bool> ObnovitZayavku(Zayavka zayavka)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string getStatusQuery = "SELECT Status FROM Zayavki WHERE ZayavkaId = @ZayavkaId";
                string oldStatus = "";

                using (SqlCommand getCmd = new SqlCommand(getStatusQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@ZayavkaId", zayavka.ZayavkaId);
                    var result = await getCmd.ExecuteScalarAsync();
                    oldStatus = result?.ToString() ?? "";
                }

                string query = @"
                    UPDATE Zayavki 
                    SET OborudovanieId = @OborudovanieId,
                        TipNeispravnostiId = @TipNeispravnostiId,
                        Opisanie = @Opisanie,
                        Status = @Status,
                        NaznachenoKomu = @NaznachenoKomu
                    WHERE ZayavkaId = @ZayavkaId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ZayavkaId", zayavka.ZayavkaId);
                    command.Parameters.AddWithValue("@OborudovanieId", zayavka.OborudovanieId);
                    command.Parameters.AddWithValue("@TipNeispravnostiId", zayavka.TipNeispravnostiId);
                    command.Parameters.AddWithValue("@Opisanie", zayavka.Opisanie);
                    command.Parameters.AddWithValue("@Status", zayavka.Status);
                    command.Parameters.AddWithValue("@NaznachenoKomu", zayavka.NaznachenoKomu.HasValue ? (object)zayavka.NaznachenoKomu.Value : DBNull.Value);

                    int result = await command.ExecuteNonQueryAsync();

                    if (result > 0 && zayavka.Status == "Выполнено" && oldStatus != "Выполнено")
                    {
                        await DobavitQRCodeKomentariy(zayavka.ZayavkaId, zayavka.NaznachenoKomu ?? 1);
                    }

                    return result > 0;
                }
            }
        }

        public async Task<bool> UdalitZayavku(int zayavkaId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Zayavki WHERE ZayavkaId = @ZayavkaId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ZayavkaId", zayavkaId);
                    int result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public async Task<List<Komentariy>> PoluchitKomentarii(int zayavkaId)
        {
            List<Komentariy> list = new List<Komentariy>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT k.*, p.PolnoeImya 
                    FROM Komentarii k
                    JOIN Polzovateli p ON k.PolzovatelId = p.PolzovatelId
                    WHERE k.ZayavkaId = @ZayavkaId
                    ORDER BY k.DataSozdaniya";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ZayavkaId", zayavkaId);

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new Komentariy
                            {
                                KomentariyId = reader.GetInt32(0),
                                ZayavkaId = reader.GetInt32(1),
                                PolzovatelId = reader.GetInt32(2),
                                TekstKomentariya = reader.GetString(3),
                                DataSozdaniya = reader.GetDateTime(4),
                                ImyaPolzovatelya = reader.GetString(5)
                            });
                        }
                    }
                }
            }

            return list;
        }

        public async Task<bool> DobavitKomentariy(Komentariy komentariy)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO Komentarii (ZayavkaId, PolzovatelId, TekstKomentariya, DataSozdaniya) 
                    VALUES (@ZayavkaId, @PolzovatelId, @TekstKomentariya, @DataSozdaniya)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ZayavkaId", komentariy.ZayavkaId);
                    command.Parameters.AddWithValue("@PolzovatelId", komentariy.PolzovatelId);
                    command.Parameters.AddWithValue("@TekstKomentariya", komentariy.TekstKomentariya);
                    command.Parameters.AddWithValue("@DataSozdaniya", DateTime.Now);

                    int result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public async Task<bool> UdalitKomentariy(int komentariyId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Komentarii WHERE KomentariyId = @KomentariyId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@KomentariyId", komentariyId);
                    int result = await command.ExecuteNonQueryAsync();
                    return result > 0;
                }
            }
        }

        public async Task<Statistics> PoluchitStatistiku()
        {
            Statistics stats = new Statistics();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query1 = "SELECT COUNT(*) FROM Zayavki";
                using (SqlCommand cmd = new SqlCommand(query1, connection))
                {
                    stats.TotalRequests = (int)await cmd.ExecuteScalarAsync();
                }

                string query2 = "SELECT COUNT(*) FROM Zayavki WHERE Status = 'Выполнено'";
                using (SqlCommand cmd = new SqlCommand(query2, connection))
                {
                    stats.CompletedRequests = (int)await cmd.ExecuteScalarAsync();
                }

                string query3 = "SELECT COUNT(*) FROM Zayavki WHERE Status = 'В работе'";
                using (SqlCommand cmd = new SqlCommand(query3, connection))
                {
                    stats.InProgressRequests = (int)await cmd.ExecuteScalarAsync();
                }

                string query4 = "SELECT COUNT(*) FROM Zayavki WHERE Status = 'В ожидании'";
                using (SqlCommand cmd = new SqlCommand(query4, connection))
                {
                    stats.WaitingRequests = (int)await cmd.ExecuteScalarAsync();
                }
            }

            return stats;
        }
    }
}