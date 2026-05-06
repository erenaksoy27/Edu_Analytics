using EduAnalytics.Core.Entities;
using EduAnalytics.Core.Enums;
using EduAnalytics.DataAccess.Context;

namespace EduAnalytics.DataAccess.Seed;

/// <summary>
/// Tamamen TEMİZ başlangıç. Sadece bir tane zorunlu "default" öğretmen kullanıcı
/// oluşturur — login sistemi olmadığı için CreatedByUserId alanı bu kullanıcıya
/// referans verir. Program / Ders / Konu / Öğrenci / PÇ / ÖÇ / Soru / Sınav
/// gibi tüm akademik veriler kullanıcı tarafından UI üzerinden eklenir.
///
/// İlk açılışta DB yoksa oluşturulur. Mevcut DB ASLA silinmez — kullanıcı verileri
/// uygulama yeniden başlasa bile korunur.
/// </summary>
public static class DbInitializer
{
    public static void Seed(EduAnalyticsDbContext context)
    {
        // DB yoksa oluştur. Varsa dokunma — kullanıcı verileri korunur.
        context.Database.EnsureCreated();

        // En az 1 öğretmen kullanıcı zorunlu (CreatedByUserId için).
        if (!context.Users.Any())
        {
            context.Users.Add(new User
            {
                FullName = "Demo Öğretmen",
                Email = "ogretmen@edu.tr",
                PasswordHash = "SHA256_PLACEHOLDER_NOT_REAL",
                Role = UserRole.Teacher,
                CreatedAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }

        // Program (Bölüm) ve Ders (Course) Yoksa Ekle
        if (!context.Programs.Any())
        {
            var program = new Program
            {
                Name = "Yazılım Mühendisliği",
                Code = "YZM"
            };
            context.Programs.Add(program);
            context.SaveChanges();

            // P1..P15 Ekle
            var outcomeDescriptions = new[]
            {
                "Matematik, fen bilimleri ve Yazılım Mühendisliği konularında yeterli bilgi birikimi",
                "Bu alanlardaki kuramsal ve uygulamalı bilgileri mühendislik problemlerini modelleme ve çözme için uygulayabilme becerisi",
                "Karmaşık Yazılım Mühendisliği problemlerini saptama, tanımlama, formüle etme ve çözme becerisi; bu amaçla uygun analiz ve modelleme yöntemlerini seçme ve uygulama becerisi",
                "Karmaşık bir sistemi, süreci, cihazı veya ürünü gerçekçi kısıtlar ve koşullar altında, belirli gereksinimleri karşılayacak şekilde tasarlama becerisi; bu amaçla modern tasarım yöntemlerini uygulama becerisi",
                "Yazılım Mühendisliği uygulamaları için gerekli olan modern yöntem ve araçları geliştirme, seçme ve kullanma becerisi; bilişim teknolojilerini etkin bir şekilde kullanma becerisi",
                "Yazılım Mühendisliği problemlerinin incelenmesi için deney tasarlama, deney yapma, veri toplama, sonuçları analiz etme ve yorumlama becerisi",
                "Disiplin içi ve çok disiplinli takımlarda etkin biçimde çalışabilme becerisi; bireysel çalışma becerisi",
                "Sözlü ve yazılı etkin iletişim kurma becerisi; en az bir yabancı dil bilgisi",
                "Yaşam boyu öğrenmenin gerekliliği bilinci; bilgiye erişebilme, bilim ve teknolojideki gelişmeleri izleme ve kendini sürekli yenileme becerisi",
                "Mesleki ve etik sorumluluk bilinci",
                "Proje yönetimi ile risk yönetimi ve değişiklik yönetimi gibi iş hayatındaki uygulamalar hakkında bilgi",
                "Girişimcilik, yenilikçilik ve sürdürebilir kalkınma hakkında farkındalık",
                "Mühendislik uygulamalarının evrensel ve toplumsal boyutlarda sağlık, çevre ve güvenlik üzerindeki etkileri hakkında bilgi",
                "Çağın sorunları hakkında bilgi",
                "Mühendislik çözümlerinin hukuksal sonuçları konusunda farkındalık"
            };

            var pList = new List<ProgramOutcome>();
            for (int i = 0; i < outcomeDescriptions.Length; i++)
            {
                pList.Add(new ProgramOutcome
                {
                    ProgramId = program.Id,
                    Code = $"P{i + 1}",
                    Description = outcomeDescriptions[i]
                });
            }
            context.ProgramOutcomes.AddRange(pList);
            context.SaveChanges();

            // Ders Ekle (Yazılım Kalite Güvencesi ve Testi)
            var course = new Course
            {
                ProgramId = program.Id,
                Code = "YM 344",
                Name = "Yazılım Kalite Güvencesi ve Testi",
                CreatedByUserId = context.Users.First().Id
            };
            context.Courses.Add(course);
            context.SaveChanges();

            // Ö1..Ö7 Ekle
            var oList = new List<LearningOutcome>();
            for (int i = 1; i <= 7; i++)
            {
                oList.Add(new LearningOutcome
                {
                    CourseId = course.Id,
                    Code = $"Ö{i}",
                    Name = $"Öğrenim Çıktısı Başlığı {i}",
                    Description = $"Öğrenim Çıktısı {i}"
                });
            }
            context.LearningOutcomes.AddRange(oList);
            context.SaveChanges();

            // Matris (İlişkiler)
            // Satırlar Ö1..Ö7, Sütunlar P1..P15 matrisi
            var matrix = new int[,]
            {
                { 4, 3, 4, 3, 3, 2, 4, 3, 4, 3, 2, 3, 4, 3, 4 }, // Ö1
                { 4, 3, 4, 3, 3, 3, 5, 4, 2, 3, 4, 4, 3, 4, 2 }, // Ö2
                { 4, 3, 4, 4, 4, 3, 3, 5, 2, 4, 2, 3, 4, 5, 3 }, // Ö3
                { 4, 3, 4, 3, 3, 2, 2, 4, 4, 4, 3, 5, 3, 2, 4 }, // Ö4
                { 4, 3, 4, 3, 4, 4, 4, 2, 3, 3, 3, 4, 4, 3, 5 }, // Ö5
                { 4, 3, 4, 3, 4, 2, 3, 4, 3, 5, 3, 4, 4, 4, 5 }, // Ö6
                { 3, 3, 4, 3, 2, 3, 5, 3, 4, 4, 3, 2, 3, 4, 4 }  // Ö7
            };

            var relations = new List<ProgramOutcomeMapping>();
            for (int oIdx = 0; oIdx < 7; oIdx++)
            {
                for (int pIdx = 0; pIdx < 15; pIdx++)
                {
                    var contribution = matrix[oIdx, pIdx];
                    if (contribution > 0)
                    {
                        relations.Add(new ProgramOutcomeMapping
                        {
                            LearningOutcomeId = oList[oIdx].Id,
                            ProgramOutcomeId = pList[pIdx].Id,
                            ContributionLevel = contribution
                        });
                    }
                }
            }

            context.ProgramOutcomeMappings.AddRange(relations);
            context.SaveChanges();
        }
    }
}
