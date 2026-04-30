using EduAnalytics.Core.Entities;
using EduAnalytics.Core.Enums;
using EduAnalytics.DataAccess.Context;

namespace EduAnalytics.DataAccess.Seed;

/// <summary>
/// ASOS yapısına göre yeniden kurgulanmış seed. Çoklu program desteği ile.
/// Hiyerarşi: Program → ProgramOutcome → LearningOutcome → Topic / Question.
/// Soru bankası ders bazlı; sınava ExamQuestion köprüsüyle bağlanır.
///
/// Demo Senaryolar (eski analiz testleri korunsun diye):
///   SQA101 Vize → Soru 4 (Cyclomatic): Çeldirici C (%83 yanlış toplama).
///   SQA101 Vize → Soru 7 (Regresyon): Düşük başarı (~%28).
/// </summary>
public static class DbInitializer
{
    public static void Seed(EduAnalyticsDbContext context)
    {
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        if (context.Users.Any())
            return;

        // ════════════════════════════════════════════════
        // 1) KULLANICI
        // ════════════════════════════════════════════════
        var teacher = new User
        {
            FullName = "Dr. Eren Aksoy",
            Email = "eren.aksoy@edu.tr",
            PasswordHash = "SHA256_PLACEHOLDER_NOT_REAL",
            Role = UserRole.Teacher,
            CreatedAt = new DateTime(2025, 9, 1, 8, 0, 0, DateTimeKind.Utc)
        };
        context.Users.Add(teacher);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 2) PROGRAMLAR (Çoklu Program)
        // ════════════════════════════════════════════════
        var bilProgram = new Program
        {
            Code = "BIL",
            Name = "Bilgisayar Mühendisliği",
            Description = "Bilgisayar mühendisliği lisans programı."
        };
        var tipProgram = new Program
        {
            Code = "TIP",
            Name = "Tıp Fakültesi",
            Description = "Tıp fakültesi lisans programı."
        };
        context.Programs.AddRange(bilProgram, tipProgram);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 3) PROGRAM ÇIKTILARI (PÇ)
        // ════════════════════════════════════════════════
        var bilOutcomes = new List<ProgramOutcome>
        {
            new() { ProgramId = bilProgram.Id, Code = "PÇ-1", Description = "Yazılım mühendisliği temel ilkelerini uygular." },
            new() { ProgramId = bilProgram.Id, Code = "PÇ-2", Description = "Algoritma ve veri yapıları kullanarak çözüm üretir." },
            new() { ProgramId = bilProgram.Id, Code = "PÇ-3", Description = "Yazılım kalitesi ve test teknikleri ile çalışır." },
            new() { ProgramId = bilProgram.Id, Code = "PÇ-4", Description = "Mühendislik etiği ve mesleki sorumluluk üstlenir." }
        };
        var tipOutcomes = new List<ProgramOutcome>
        {
            new() { ProgramId = tipProgram.Id, Code = "PÇ-1", Description = "Temel tıp bilimlerini hasta yönetiminde uygular." },
            new() { ProgramId = tipProgram.Id, Code = "PÇ-2", Description = "Klinik akıl yürütme ve karar verme süreçlerini yönetir." },
            new() { ProgramId = tipProgram.Id, Code = "PÇ-3", Description = "Tıbbi etik ve hasta haklarını gözetir." }
        };
        context.ProgramOutcomes.AddRange(bilOutcomes);
        context.ProgramOutcomes.AddRange(tipOutcomes);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 4) DERSLER
        // ════════════════════════════════════════════════
        var sqaCourse = new Course
        {
            ProgramId = bilProgram.Id,
            Name = "Yazılım Kalite ve Güvencesi",
            Code = "SQA101",
            Description = "Yazılım test süreçleri, kalite metrikleri, otomasyon araçları.",
            CreatedByUserId = teacher.Id
        };
        var algCourse = new Course
        {
            ProgramId = bilProgram.Id,
            Name = "Veri Yapıları ve Algoritmalar",
            Code = "CS201",
            Description = "Temel veri yapıları ve algoritmik düşünce.",
            CreatedByUserId = teacher.Id
        };
        var anaCourse = new Course
        {
            ProgramId = tipProgram.Id,
            Name = "Anatomi",
            Code = "ANA101",
            Description = "İnsan anatomisi temelleri.",
            CreatedByUserId = teacher.Id
        };
        context.Courses.AddRange(sqaCourse, algCourse, anaCourse);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 5) ÖĞRENİM ÇIKTILARI (ÖÇ) — Ders bazlı
        // ════════════════════════════════════════════════
        var sqaOutcomes = new List<LearningOutcome>
        {
            new() { CourseId = sqaCourse.Id, Code = "ÖÇ-1", Name = "Test seviyelerini ayırt etme", Description = "Birim, entegrasyon, sistem ve kabul testlerini birbirinden ayırır." },
            new() { CourseId = sqaCourse.Id, Code = "ÖÇ-2", Name = "Statik analiz uygulama",       Description = "Kod inceleme ve metrik tabanlı kalite ölçümü yapar." },
            new() { CourseId = sqaCourse.Id, Code = "ÖÇ-3", Name = "Test otomasyonu yazma",       Description = "Otomatik test araçlarıyla tekrarlanabilir testler oluşturur." },
            new() { CourseId = sqaCourse.Id, Code = "ÖÇ-4", Name = "CI/CD entegrasyonu",          Description = "Sürekli entegrasyon hatlarına test entegre eder." },
            new() { CourseId = sqaCourse.Id, Code = "ÖÇ-5", Name = "Kapsama metriklerini yorumlama", Description = "Kod kapsama oranını doğru şekilde değerlendirir." }
        };
        var algOutcomes = new List<LearningOutcome>
        {
            new() { CourseId = algCourse.Id, Code = "ÖÇ-1", Name = "Big-O analizi", Description = "Algoritma karmaşıklığını ifade eder." },
            new() { CourseId = algCourse.Id, Code = "ÖÇ-2", Name = "Ağaç yapıları", Description = "BST, AVL ve Heap yapılarını uygular." },
            new() { CourseId = algCourse.Id, Code = "ÖÇ-3", Name = "Çizge algoritmaları", Description = "BFS, DFS, en kısa yol algoritmalarını uygular." }
        };
        var anaOutcomes = new List<LearningOutcome>
        {
            new() { CourseId = anaCourse.Id, Code = "ÖÇ-1", Name = "Kemik sistemi", Description = "İskelet sistemini tanır." },
            new() { CourseId = anaCourse.Id, Code = "ÖÇ-2", Name = "Kas sistemi",   Description = "Kasların yapı ve işlevlerini açıklar." },
            new() { CourseId = anaCourse.Id, Code = "ÖÇ-3", Name = "Sinir sistemi", Description = "Periferik ve merkezi sinir sistemini ayırt eder." }
        };
        context.LearningOutcomes.AddRange(sqaOutcomes);
        context.LearningOutcomes.AddRange(algOutcomes);
        context.LearningOutcomes.AddRange(anaOutcomes);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 6) PROGRAM ÇIKTI ↔ DERS ÇIKTI EŞLEŞMELERİ
        // ════════════════════════════════════════════════
        var poMappings = new List<ProgramOutcomeMapping>
        {
            // BIL PÇ-3 (Yazılım kalitesi) ↔ SQA tüm ÖÇ'leri
            new() { ProgramOutcomeId = bilOutcomes[2].Id, LearningOutcomeId = sqaOutcomes[0].Id, ContributionLevel = 5 },
            new() { ProgramOutcomeId = bilOutcomes[2].Id, LearningOutcomeId = sqaOutcomes[1].Id, ContributionLevel = 4 },
            new() { ProgramOutcomeId = bilOutcomes[2].Id, LearningOutcomeId = sqaOutcomes[2].Id, ContributionLevel = 5 },
            new() { ProgramOutcomeId = bilOutcomes[2].Id, LearningOutcomeId = sqaOutcomes[3].Id, ContributionLevel = 4 },
            new() { ProgramOutcomeId = bilOutcomes[2].Id, LearningOutcomeId = sqaOutcomes[4].Id, ContributionLevel = 3 },
            // BIL PÇ-1 (Yazılım mühendisliği temelleri) ↔ SQA ÖÇ-1, ÖÇ-2
            new() { ProgramOutcomeId = bilOutcomes[0].Id, LearningOutcomeId = sqaOutcomes[0].Id, ContributionLevel = 3 },
            new() { ProgramOutcomeId = bilOutcomes[0].Id, LearningOutcomeId = sqaOutcomes[1].Id, ContributionLevel = 3 },
            // BIL PÇ-2 (Algoritmalar) ↔ ALG ÖÇ'leri
            new() { ProgramOutcomeId = bilOutcomes[1].Id, LearningOutcomeId = algOutcomes[0].Id, ContributionLevel = 5 },
            new() { ProgramOutcomeId = bilOutcomes[1].Id, LearningOutcomeId = algOutcomes[1].Id, ContributionLevel = 4 },
            new() { ProgramOutcomeId = bilOutcomes[1].Id, LearningOutcomeId = algOutcomes[2].Id, ContributionLevel = 5 },
            // TIP PÇ-1 (Temel tıp) ↔ ANA ÖÇ'leri
            new() { ProgramOutcomeId = tipOutcomes[0].Id, LearningOutcomeId = anaOutcomes[0].Id, ContributionLevel = 5 },
            new() { ProgramOutcomeId = tipOutcomes[0].Id, LearningOutcomeId = anaOutcomes[1].Id, ContributionLevel = 5 },
            new() { ProgramOutcomeId = tipOutcomes[0].Id, LearningOutcomeId = anaOutcomes[2].Id, ContributionLevel = 4 }
        };
        context.ProgramOutcomeMappings.AddRange(poMappings);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 7) KONULAR (Haftalık Plan)
        // ════════════════════════════════════════════════
        var sqaTopic1 = new Topic { CourseId = sqaCourse.Id, WeekNumber = 3, Title = "Test Seviyeleri",          Description = "Birim, entegrasyon, sistem, kabul testleri." };
        var sqaTopic2 = new Topic { CourseId = sqaCourse.Id, WeekNumber = 5, Title = "Statik Analiz",            Description = "Kod inceleme, statik analiz araçları, metrikler." };
        var sqaTopic3 = new Topic { CourseId = sqaCourse.Id, WeekNumber = 8, Title = "Test Otomasyon Araçları",  Description = "Selenium, xUnit, CI/CD entegrasyonu." };
        context.Topics.AddRange(sqaTopic1, sqaTopic2, sqaTopic3);

        var algTopics = new[]
        {
            new Topic { CourseId = algCourse.Id, WeekNumber = 1, Title = "Algoritma Analizi ve Big O", Description = "Karmaşıklık analizi." },
            new Topic { CourseId = algCourse.Id, WeekNumber = 4, Title = "İkili Arama Ağaçları",       Description = "BST yapısı." },
            new Topic { CourseId = algCourse.Id, WeekNumber = 9, Title = "Çizge Gezinme",              Description = "BFS, DFS." }
        };
        context.Topics.AddRange(algTopics);

        var anaTopic1 = new Topic { CourseId = anaCourse.Id, WeekNumber = 2, Title = "Üst Ekstremite Kemikleri", Description = "Klavikula, humerus, ulna, radius." };
        var anaTopic2 = new Topic { CourseId = anaCourse.Id, WeekNumber = 4, Title = "Üst Ekstremite Kasları",   Description = "Omuz ve kol kasları." };
        context.Topics.AddRange(anaTopic1, anaTopic2);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 8) KONU ↔ ÖĞRENİM ÇIKTISI
        // ════════════════════════════════════════════════
        var topicLoLinks = new List<TopicLearningOutcome>
        {
            new() { TopicId = sqaTopic1.Id, LearningOutcomeId = sqaOutcomes[0].Id }, // Test Seviyeleri ↔ ÖÇ-1
            new() { TopicId = sqaTopic2.Id, LearningOutcomeId = sqaOutcomes[1].Id }, // Statik Analiz  ↔ ÖÇ-2
            new() { TopicId = sqaTopic2.Id, LearningOutcomeId = sqaOutcomes[4].Id }, // Statik Analiz  ↔ ÖÇ-5 (kapsama)
            new() { TopicId = sqaTopic3.Id, LearningOutcomeId = sqaOutcomes[2].Id }, // Otomasyon      ↔ ÖÇ-3
            new() { TopicId = sqaTopic3.Id, LearningOutcomeId = sqaOutcomes[3].Id }, // Otomasyon      ↔ ÖÇ-4 (CI/CD)

            new() { TopicId = anaTopic1.Id, LearningOutcomeId = anaOutcomes[0].Id }, // Kemikler ↔ ÖÇ-1
            new() { TopicId = anaTopic2.Id, LearningOutcomeId = anaOutcomes[1].Id }  // Kaslar   ↔ ÖÇ-2
        };
        context.TopicLearningOutcomes.AddRange(topicLoLinks);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 9) SORU BANKASI (SQA dersi için 12 bağımsız soru)
        // ════════════════════════════════════════════════
        var qBank = new List<Question>
        {
            new() { CourseId = sqaCourse.Id, QuestionText = "Birim testi (unit test) hangi seviyede yapılır?",
                OptionA = "Sistem seviyesinde", OptionB = "Fonksiyon/metot seviyesinde",
                OptionC = "Kabul testi seviyesinde", OptionD = "Entegrasyon seviyesinde",
                CorrectOption = OptionLetter.B, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Entegrasyon testinin temel amacı nedir?",
                OptionA = "Kullanıcı arayüzünü test etmek", OptionB = "Tek bir fonksiyonun doğruluğunu kontrol etmek",
                OptionC = "Performans ölçümü yapmak", OptionD = "Modüller arası etkileşimleri doğrulamak",
                CorrectOption = OptionLetter.D, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Statik analiz ile tespit edilemeyen ancak birim testleriyle bulunabilecek hata türü?",
                OptionA = "Çalışma zamanı mantık hataları", OptionB = "Kod standart ihlalleri",
                OptionC = "Kullanılmayan değişkenler", OptionD = "Yanlış isimlendirme kalıpları",
                CorrectOption = OptionLetter.A, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Cyclomatic Complexity (Döngüsel Karmaşıklık) neyi ölçer?",
                OptionA = "Koddaki bağımsız yol sayısını", OptionB = "Satır başına hata oranını",
                OptionC = "Kod tekrar (duplication) oranını", OptionD = "Fonksiyon çağrı derinliğini",
                CorrectOption = OptionLetter.A, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "SonarQube hangi amaçla kullanılır?",
                OptionA = "Performans testi", OptionB = "Yük testi",
                OptionC = "Statik kod analizi ve kalite ölçümü", OptionD = "Veritabanı testi",
                CorrectOption = OptionLetter.C, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Selenium aracı öncelikli olarak ne için kullanılır?",
                OptionA = "API testi", OptionB = "Web tarayıcı tabanlı otomasyon testi",
                OptionC = "Birim testi", OptionD = "Güvenlik testi",
                CorrectOption = OptionLetter.B, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Regresyon testlerinin CI/CD pipeline'ına entegre edilmesinin en önemli faydası nedir?",
                OptionA = "Geliştirme maliyetini düşürür", OptionB = "Kod satır sayısını azaltır",
                OptionC = "Manuel test ihtiyacını tamamen ortadan kaldırır",
                OptionD = "Her değişiklikte mevcut fonksiyonların bozulmadığını otomatik doğrular",
                CorrectOption = OptionLetter.D, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "xUnit framework'ünde [Fact] attribute'ü ne anlama gelir?",
                OptionA = "Parametresiz, tek bir test vakasını tanımlar",
                OptionB = "Parametreli test vakası tanımlar", OptionC = "Test sınıfını işaretler",
                OptionD = "Test sonucunu loglar",
                CorrectOption = OptionLetter.A, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Kod kapsama (code coverage) oranı %100 olduğunda ne söylenebilir?",
                OptionA = "Yazılımda hiç hata yoktur", OptionB = "Tüm kullanıcı senaryoları test edilmiştir",
                OptionC = "Tüm kod satırları en az bir kez çalıştırılmıştır ancak hatasızlık garanti değildir",
                OptionD = "Performans testleri tamamlanmıştır",
                CorrectOption = OptionLetter.C, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, QuestionText = "Kabul testi (acceptance test) kimler tarafından yürütülür?",
                OptionA = "Sadece yazılım geliştiriciler", OptionB = "Son kullanıcılar veya müşteri temsilcileri",
                OptionC = "Sadece test mühendisleri", OptionD = "Veritabanı yöneticileri",
                CorrectOption = OptionLetter.B, CreatedByUserId = teacher.Id, IsFavorite = true },

            // ── Klasik (open-ended) sorular ──
            new() { CourseId = sqaCourse.Id, Type = QuestionType.OpenEnded, MaxPoints = 10.0m,
                QuestionText = "Birim, entegrasyon ve sistem testinin farklarını örneklerle açıklayınız.",
                AnswerKey = "Birim: tek metot izole. Entegrasyon: modüller arası. Sistem: uçtan uca. Her biri için örnek beklenir.",
                CorrectOption = OptionLetter.Empty, CreatedByUserId = teacher.Id },
            new() { CourseId = sqaCourse.Id, Type = QuestionType.OpenEnded, MaxPoints = 15.0m,
                QuestionText = "Kod kapsama %95 olmasına rağmen üretimde kritik bir hata çıktı. Yorumlayın.",
                AnswerKey = "Coverage ≠ kalite. Edge case'ler / mutation testing / property-based testing değerlendirilmeli.",
                CorrectOption = OptionLetter.Empty, CreatedByUserId = teacher.Id }
        };
        context.Questions.AddRange(qBank);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 10) ANATOMİ DERSİ — COMMON-STEM (İlişkili Soru) ÖRNEĞİ
        // ════════════════════════════════════════════════
        var anaGroup = new QuestionGroup
        {
            CourseId = anaCourse.Id,
            StemText = "Aşağıdaki tablo, üst ekstremitedeki başlıca kemiklerin uzunluk ve eklem ilişkilerini özetler.\n" +
                       "  • Klavikula  — sternoklaviküler ve akromioklaviküler eklem\n" +
                       "  • Humerus    — glenohumeral ve dirsek eklemi\n" +
                       "  • Ulna       — dirsek ve el bileği eklemi (medial taraf)\n" +
                       "  • Radius     — dirsek ve el bileği eklemi (lateral taraf)\n" +
                       "Tabloya göre soruları yanıtlayınız.",
            MediaPath = null, // İsteğe bağlı görsel daha sonra eklenebilir
            CreatedByUserId = teacher.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.QuestionGroups.Add(anaGroup);
        context.SaveChanges();

        var anaQuestions = new List<Question>
        {
            new()
            {
                CourseId = anaCourse.Id, QuestionGroupId = anaGroup.Id,
                QuestionText = "Tabloya göre, glenohumeral eklemi oluşturan kemik aşağıdakilerden hangisidir?",
                OptionA = "Klavikula", OptionB = "Humerus", OptionC = "Ulna",
                OptionD = "Radius",    OptionE = "Skapula",
                CorrectOption = OptionLetter.B, CreatedByUserId = teacher.Id
            },
            new()
            {
                CourseId = anaCourse.Id, QuestionGroupId = anaGroup.Id,
                QuestionText = "Tabloya göre, el bileği ekleminin lateralinde yer alan kemik hangisidir?",
                OptionA = "Klavikula", OptionB = "Humerus", OptionC = "Ulna",
                OptionD = "Radius",    OptionE = "Skapula",
                CorrectOption = OptionLetter.D, CreatedByUserId = teacher.Id
            },
            new()
            {
                CourseId = anaCourse.Id, QuestionGroupId = anaGroup.Id,
                QuestionText = "Tabloya göre, sternoklaviküler ekleme katılan kemik aşağıdakilerden hangisidir?",
                OptionA = "Klavikula", OptionB = "Humerus", OptionC = "Ulna",
                OptionD = "Radius",    OptionE = "Skapula",
                CorrectOption = OptionLetter.A, CreatedByUserId = teacher.Id
            }
        };
        context.Questions.AddRange(anaQuestions);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 11) SORU ↔ KONU ve SORU ↔ ÖÇ İLİŞKİLERİ
        // ════════════════════════════════════════════════
        var qTopics = new List<QuestionTopic>
        {
            new() { QuestionId = qBank[0].Id, TopicId = sqaTopic1.Id },
            new() { QuestionId = qBank[1].Id, TopicId = sqaTopic1.Id },
            new() { QuestionId = qBank[2].Id, TopicId = sqaTopic1.Id },
            new() { QuestionId = qBank[2].Id, TopicId = sqaTopic2.Id },
            new() { QuestionId = qBank[3].Id, TopicId = sqaTopic2.Id },
            new() { QuestionId = qBank[4].Id, TopicId = sqaTopic2.Id },
            new() { QuestionId = qBank[5].Id, TopicId = sqaTopic3.Id },
            new() { QuestionId = qBank[6].Id, TopicId = sqaTopic1.Id },
            new() { QuestionId = qBank[6].Id, TopicId = sqaTopic3.Id },
            new() { QuestionId = qBank[7].Id, TopicId = sqaTopic3.Id },
            new() { QuestionId = qBank[8].Id, TopicId = sqaTopic2.Id },
            new() { QuestionId = qBank[8].Id, TopicId = sqaTopic3.Id },
            new() { QuestionId = qBank[9].Id, TopicId = sqaTopic1.Id },
            new() { QuestionId = qBank[10].Id, TopicId = sqaTopic1.Id },
            new() { QuestionId = qBank[11].Id, TopicId = sqaTopic2.Id },

            new() { QuestionId = anaQuestions[0].Id, TopicId = anaTopic1.Id },
            new() { QuestionId = anaQuestions[1].Id, TopicId = anaTopic1.Id },
            new() { QuestionId = anaQuestions[2].Id, TopicId = anaTopic1.Id }
        };
        context.QuestionTopics.AddRange(qTopics);

        var qOutcomes = new List<QuestionLearningOutcome>
        {
            new() { QuestionId = qBank[0].Id, LearningOutcomeId = sqaOutcomes[0].Id },
            new() { QuestionId = qBank[1].Id, LearningOutcomeId = sqaOutcomes[0].Id },
            new() { QuestionId = qBank[2].Id, LearningOutcomeId = sqaOutcomes[0].Id },
            new() { QuestionId = qBank[2].Id, LearningOutcomeId = sqaOutcomes[1].Id },
            new() { QuestionId = qBank[3].Id, LearningOutcomeId = sqaOutcomes[1].Id },
            new() { QuestionId = qBank[4].Id, LearningOutcomeId = sqaOutcomes[1].Id },
            new() { QuestionId = qBank[5].Id, LearningOutcomeId = sqaOutcomes[2].Id },
            new() { QuestionId = qBank[6].Id, LearningOutcomeId = sqaOutcomes[3].Id },
            new() { QuestionId = qBank[7].Id, LearningOutcomeId = sqaOutcomes[2].Id },
            new() { QuestionId = qBank[8].Id, LearningOutcomeId = sqaOutcomes[4].Id },
            new() { QuestionId = qBank[9].Id, LearningOutcomeId = sqaOutcomes[0].Id },
            new() { QuestionId = qBank[10].Id, LearningOutcomeId = sqaOutcomes[0].Id },
            new() { QuestionId = qBank[11].Id, LearningOutcomeId = sqaOutcomes[4].Id },

            new() { QuestionId = anaQuestions[0].Id, LearningOutcomeId = anaOutcomes[0].Id },
            new() { QuestionId = anaQuestions[1].Id, LearningOutcomeId = anaOutcomes[0].Id },
            new() { QuestionId = anaQuestions[2].Id, LearningOutcomeId = anaOutcomes[0].Id }
        };
        context.QuestionLearningOutcomes.AddRange(qOutcomes);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 12) ÖĞRENCİLER (25 kişi)
        // ════════════════════════════════════════════════
        var students = new List<Student>
        {
            new() { StudentNumber = "2024001", FullName = "Ahmet Yılmaz",     ClassName = "Yazılım - 3.Sınıf" },
            new() { StudentNumber = "2024002", FullName = "Ayşe Kaya",        ClassName = "Bilgisayar - 4.Sınıf" },
            new() { StudentNumber = "2024003", FullName = "Mehmet Demir",     ClassName = "Yapay Zeka - 2.Sınıf" },
            new() { StudentNumber = "2024004", FullName = "Fatma Çelik",      ClassName = "Yazılım - 4.Sınıf" },
            new() { StudentNumber = "2024005", FullName = "Ali Şahin",        ClassName = "YBS - 3.Sınıf" },
            new() { StudentNumber = "2024006", FullName = "Zeynep Yıldız",    ClassName = "Elektrik - 4.Sınıf" },
            new() { StudentNumber = "2024007", FullName = "Mustafa Özdemir",  ClassName = "Bilgisayar - 3.Sınıf" },
            new() { StudentNumber = "2024008", FullName = "Elif Arslan",      ClassName = "Yazılım - 3.Sınıf" },
            new() { StudentNumber = "2024009", FullName = "Hasan Doğan",      ClassName = "Yazılım - 2.Sınıf" },
            new() { StudentNumber = "2024010", FullName = "Merve Kılıç",      ClassName = "YBS - 4.Sınıf" },
            new() { StudentNumber = "2024011", FullName = "Hüseyin Aydın",    ClassName = "Yapay Zeka - 3.Sınıf" },
            new() { StudentNumber = "2024012", FullName = "Büşra Öztürk",     ClassName = "Bilgisayar - 2.Sınıf" },
            new() { StudentNumber = "2024013", FullName = "İbrahim Çetin",    ClassName = "Yazılım - 3.Sınıf" },
            new() { StudentNumber = "2024014", FullName = "Seda Koç",         ClassName = "Elektrik - 3.Sınıf" },
            new() { StudentNumber = "2024015", FullName = "Emre Kara",        ClassName = "Yazılım - 4.Sınıf" },
            new() { StudentNumber = "2024016", FullName = "Gizem Aksoy",      ClassName = "Bilgisayar - 4.Sınıf" },
            new() { StudentNumber = "2024017", FullName = "Burak Polat",      ClassName = "YBS - 2.Sınıf" },
            new() { StudentNumber = "2024018", FullName = "Esra Erdoğan",     ClassName = "Yapay Zeka - 4.Sınıf" },
            new() { StudentNumber = "2024019", FullName = "Oğuz Tekin",       ClassName = "Bilgisayar - 3.Sınıf" },
            new() { StudentNumber = "2024020", FullName = "Gamze Güneş",      ClassName = "Yazılım - 2.Sınıf" },
            new() { StudentNumber = "2024021", FullName = "Cem Korkmaz",      ClassName = "Elektrik - 4.Sınıf" },
            new() { StudentNumber = "2024022", FullName = "Derya Yılmazer",   ClassName = "Yazılım - 3.Sınıf" },
            new() { StudentNumber = "2024023", FullName = "Serkan Aktaş",     ClassName = "Bilgisayar - 3.Sınıf" },
            new() { StudentNumber = "2024024", FullName = "Tuğba Şen",        ClassName = "YBS - 3.Sınıf" },
            new() { StudentNumber = "2024025", FullName = "Furkan Başaran",   ClassName = "Yazılım - 4.Sınıf" }
        };
        context.Students.AddRange(students);
        context.SaveChanges();

        var enrollments = students.Select(s => new StudentCourse
        {
            StudentId = s.Id,
            CourseId = sqaCourse.Id
        }).ToList();
        context.StudentCourses.AddRange(enrollments);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 13) SQA101 VİZE SINAVI
        // ════════════════════════════════════════════════
        var sqaExam = new Exam
        {
            CourseId = sqaCourse.Id,
            Title = "SQA101 Vize Sınavı",
            ExamDate = new DateTime(2025, 11, 15, 10, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 90,
            ExamType = ExamType.Midterm,
            BookletCount = 1,
            ShuffleOptions = false,
            CreatedByUserId = teacher.Id
        };
        context.Exams.Add(sqaExam);
        context.SaveChanges();

        // ExamQuestion köprüsü ile soru havuzundan 12 soru seç
        var examQuestions = new List<ExamQuestion>();
        for (int i = 0; i < qBank.Count; i++)
        {
            examQuestions.Add(new ExamQuestion
            {
                ExamId = sqaExam.Id,
                QuestionId = qBank[i].Id,
                OrderInExam = i + 1,
                OverrideMaxPoints = null,
                IsCancelled = false
            });
        }
        context.ExamQuestions.AddRange(examQuestions);
        context.SaveChanges();

        // Tek kitapçık
        var sqaBooklet = new ExamBooklet
        {
            ExamId = sqaExam.Id,
            BookletCode = "A"
        };
        context.ExamBooklets.Add(sqaBooklet);
        context.SaveChanges();

        var bookletQs = qBank.Select((q, idx) => new ExamBookletQuestion
        {
            BookletId = sqaBooklet.Id,
            QuestionId = q.Id,
            OrderInBooklet = idx + 1,
            OptionShuffleMap = null
        }).ToList();
        context.ExamBookletQuestions.AddRange(bookletQs);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 14) ÖĞRENCİ CEVAPLARI (analiz senaryoları korunmuş matris)
        // ════════════════════════════════════════════════
        var A = OptionLetter.A; var B = OptionLetter.B; var C = OptionLetter.C; var D = OptionLetter.D;

        OptionLetter[][] answerMatrix =
        [
            /*Q1  B*/ [ B,B,A,B,B,B,C,B,B,A,B,B,D,B,B,A,B,B,C,B,D,B,B,B,B ],
            /*Q2  D*/ [ D,B,D,D,A,D,D,B,D,D,C,D,A,B,D,D,B,D,D,A,C,D,D,D,D ],
            /*Q3  A*/ [ A,B,A,A,D,A,B,C,A,A,B,A,D,C,A,B,A,A,D,A,C,B,A,A,A ],
            /*Q4  A — çeldirici C*/ [ C,C,A,C,C,C,A,C,C,B,C,A,C,C,C,A,D,C,C,A,B,C,A,C,A ],
            /*Q5  C*/ [ C,C,A,C,C,B,C,C,D,C,C,A,C,B,C,C,C,A,D,C,B,C,C,C,C ],
            /*Q6  B*/ [ B,A,B,C,B,B,D,A,B,B,C,B,A,B,D,B,B,C,B,A,D,B,B,B,B ],
            /*Q7  D — düşük başarı*/ [ A,C,C,D,B,A,C,B,A,C,D,B,A,C,D,A,B,C,D,C,A,B,D,D,D ],
            /*Q8  A*/ [ A,A,A,B,A,A,A,C,A,A,A,A,D,A,A,A,B,A,A,C,A,D,A,A,A ],
            /*Q9  C*/ [ C,B,A,C,C,B,D,A,C,C,B,C,A,B,C,D,C,C,B,A,C,D,C,C,C ],
            /*Q10 B*/ [ B,B,B,A,B,B,B,B,C,B,B,B,A,B,B,B,B,D,B,B,C,B,B,B,B ]
        ];
        OptionLetter[] correctMc = [B, D, A, A, C, B, D, A, C, B];

        var studentAnswers = new List<StudentAnswer>();

        for (int qi = 0; qi < 10; qi++)
        {
            for (int si = 0; si < 25; si++)
            {
                var sel = answerMatrix[qi][si];
                studentAnswers.Add(new StudentAnswer
                {
                    ExamId = sqaExam.Id,
                    QuestionId = qBank[qi].Id,
                    StudentId = students[si].Id,
                    BookletId = sqaBooklet.Id,
                    SelectedOption = sel,
                    IsCorrect = sel == correctMc[qi],
                    Score = null
                });
            }
        }

        // Klasik soru puanları
        decimal[] q11Scores = [8, 9, 4, 10, 3, 7, 6, 5, 9, 6, 4, 8, 2, 5, 7, 9, 6, 3, 8, 5, 4, 7, 10, 6, 5];
        decimal[] q12Scores = [13, 11, 5, 14, 2, 10, 8, 6, 12, 9, 4, 11, 1, 7, 10, 13, 8, 4, 11, 7, 5, 9, 15, 8, 7];

        for (int si = 0; si < 25; si++)
        {
            studentAnswers.Add(new StudentAnswer
            {
                ExamId = sqaExam.Id,
                QuestionId = qBank[10].Id,
                StudentId = students[si].Id,
                BookletId = sqaBooklet.Id,
                SelectedOption = OptionLetter.Empty,
                Score = q11Scores[si],
                IsCorrect = q11Scores[si] >= 5
            });
            studentAnswers.Add(new StudentAnswer
            {
                ExamId = sqaExam.Id,
                QuestionId = qBank[11].Id,
                StudentId = students[si].Id,
                BookletId = sqaBooklet.Id,
                SelectedOption = OptionLetter.Empty,
                Score = q12Scores[si],
                IsCorrect = q12Scores[si] >= 7.5m
            });
        }

        context.StudentAnswers.AddRange(studentAnswers);
        context.SaveChanges();

        // ════════════════════════════════════════════════
        // 15) FAZ 5 — Klasik Sorular için RUBRIC Kriterleri
        // ════════════════════════════════════════════════
        // Q11 (10 puan) → 3 kriter: Kavram Doğruluğu (4) + Örnek Verme (3) + Açıklık (3)
        var q11 = qBank[10];
        var q11Crits = new List<QuestionRubricCriterion>
        {
            new() { QuestionId = q11.Id, Title = "Kavram Doğruluğu", MaxPoints = 4.0m, Order = 1,
                    Description = "Birim/entegrasyon/sistem kavramları doğru ayrıştırılmış mı?" },
            new() { QuestionId = q11.Id, Title = "Örnek Verme",     MaxPoints = 3.0m, Order = 2,
                    Description = "Her test seviyesi için anlamlı bir örnek verilmiş mi?" },
            new() { QuestionId = q11.Id, Title = "Açıklık",         MaxPoints = 3.0m, Order = 3,
                    Description = "Anlatım net ve düzenli mi?" }
        };
        context.QuestionRubricCriteria.AddRange(q11Crits);

        // Q12 (15 puan) → 3 kriter: 5+5+5
        var q12 = qBank[11];
        var q12Crits = new List<QuestionRubricCriterion>
        {
            new() { QuestionId = q12.Id, Title = "Kapsam Sınırı Kavraması", MaxPoints = 5.0m, Order = 1,
                    Description = "Coverage'ın neden hatasızlık garantisi olmadığı açıklanmış mı?" },
            new() { QuestionId = q12.Id, Title = "Örnek/Senaryo",            MaxPoints = 5.0m, Order = 2,
                    Description = "E-ticaret bağlamında somut bir senaryo verilmiş mi?" },
            new() { QuestionId = q12.Id, Title = "Çözüm Önerisi",            MaxPoints = 5.0m, Order = 3,
                    Description = "Mutation / property-based testing gibi alternatif önerilmiş mi?" }
        };
        context.QuestionRubricCriteria.AddRange(q12Crits);
        context.SaveChanges();

        // Öğrencilerin Q11/Q12 kriter puanları (toplam puanı oransal dağıt)
        var criterionScores = new List<StudentAnswerCriterion>();
        var allClassicalAnswers = studentAnswers
            .Where(sa => sa.QuestionId == q11.Id || sa.QuestionId == q12.Id)
            .ToList();

        foreach (var sa in allClassicalAnswers)
        {
            if (sa.Score == null) continue;
            var total = sa.Score.Value;

            if (sa.QuestionId == q11.Id)
            {
                // 4:3:3 oranında dağıt (toplam 10)
                var c1 = Math.Round(total * 0.4m, 1);
                var c2 = Math.Round(total * 0.3m, 1);
                var c3 = Math.Round(total - c1 - c2, 1);
                criterionScores.Add(new StudentAnswerCriterion { StudentAnswerId = sa.Id, CriterionId = q11Crits[0].Id, Score = Math.Min(c1, 4.0m) });
                criterionScores.Add(new StudentAnswerCriterion { StudentAnswerId = sa.Id, CriterionId = q11Crits[1].Id, Score = Math.Min(c2, 3.0m) });
                criterionScores.Add(new StudentAnswerCriterion { StudentAnswerId = sa.Id, CriterionId = q11Crits[2].Id, Score = Math.Min(c3, 3.0m) });
            }
            else // q12
            {
                // 5:5:5 oranında dağıt (toplam 15)
                var c1 = Math.Round(total / 3m, 1);
                var c2 = Math.Round(total / 3m, 1);
                var c3 = Math.Round(total - c1 - c2, 1);
                criterionScores.Add(new StudentAnswerCriterion { StudentAnswerId = sa.Id, CriterionId = q12Crits[0].Id, Score = Math.Min(c1, 5.0m) });
                criterionScores.Add(new StudentAnswerCriterion { StudentAnswerId = sa.Id, CriterionId = q12Crits[1].Id, Score = Math.Min(c2, 5.0m) });
                criterionScores.Add(new StudentAnswerCriterion { StudentAnswerId = sa.Id, CriterionId = q12Crits[2].Id, Score = Math.Min(c3, 5.0m) });
            }
        }
        context.StudentAnswerCriteria.AddRange(criterionScores);
        context.SaveChanges();
    }
}
