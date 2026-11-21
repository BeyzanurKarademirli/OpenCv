namespace OpenCv
{
    /// <summary>Bir nesne sınıfı hakkında Türkçe bilgiler ve tipik gerçek boyutlar.</summary>
    public sealed class ObjectClassInfo
    {
        public ObjectClassInfo(string name, string category, double heightM, double widthM,
            string description, string wikiTitle)
        {
            Name = name;
            Category = category;
            HeightM = heightM;
            WidthM = widthM;
            Description = description;
            WikiTitle = wikiTitle;
        }

        public string Name { get; private set; }
        public string Category { get; private set; }

        /// <summary>Mesafe tahmini için kullanılan tipik yükseklik (metre).</summary>
        public double HeightM { get; private set; }

        /// <summary>Mesafe tahmini için kullanılan tipik genişlik (metre).</summary>
        public double WidthM { get; private set; }

        public string Description { get; private set; }

        /// <summary>Türkçe Vikipedi makale başlığı.</summary>
        public string WikiTitle { get; private set; }
    }

    /// <summary>
    /// COCO veri setinin 80 sınıfı (YOLO modelinin çıktı sırasıyla aynı).
    /// Boyutlar ortalama değerlerdir; mesafe tahmini bu değerlere dayanır.
    /// </summary>
    public static class ObjectCatalog
    {
        private const string Canli = "Canlı";
        private const string Hayvan = "Hayvan";
        private const string Arac = "Araç";
        private const string Trafik = "Trafik / Sokak";
        private const string Esya = "Kişisel eşya";
        private const string Spor = "Spor";
        private const string Mutfak = "Mutfak";
        private const string Yiyecek = "Yiyecek";
        private const string Mobilya = "Mobilya";
        private const string Elektronik = "Elektronik";
        private const string EvAleti = "Ev aleti";
        private const string Ev = "Ev eşyası";

        public static readonly ObjectClassInfo[] Classes =
        {
            new ObjectClassInfo("Kişi", Canli, 1.70, 0.45, "Bir insan. Mesafe, görünen boy ya da omuz genişliğinden tahmin edilir.", "İnsan"),
            new ObjectClassInfo("Bisiklet", Arac, 1.00, 1.70, "Pedalla çalışan iki tekerlekli taşıt.", "Bisiklet"),
            new ObjectClassInfo("Otomobil", Arac, 1.50, 1.80, "Yolcu taşımak için kullanılan motorlu kara taşıtı.", "Otomobil"),
            new ObjectClassInfo("Motosiklet", Arac, 1.10, 2.00, "Motorlu, iki tekerlekli taşıt.", "Motosiklet"),
            new ObjectClassInfo("Uçak", Arac, 12.00, 35.00, "Kanatlarıyla havada taşıma sağlayan motorlu hava aracı.", "Uçak"),
            new ObjectClassInfo("Otobüs", Arac, 3.20, 2.50, "Çok sayıda yolcu taşıyan büyük motorlu taşıt.", "Otobüs"),
            new ObjectClassInfo("Tren", Arac, 4.00, 3.00, "Raylar üzerinde ilerleyen vagon dizisi.", "Tren"),
            new ObjectClassInfo("Kamyon / Kamyonet", Arac, 2.50, 2.20, "Yük taşımak için tasarlanmış motorlu taşıt (kamyonetler de bu sınıfta).", "Kamyon"),
            new ObjectClassInfo("Tekne", Arac, 2.00, 5.00, "Su üzerinde yüzen küçük deniz taşıtı.", "Tekne"),
            new ObjectClassInfo("Trafik ışığı", Trafik, 0.90, 0.30, "Kavşaklarda trafiği yöneten sinyal lambası.", "Trafik ışıkları"),
            new ObjectClassInfo("Yangın musluğu", Trafik, 0.75, 0.40, "İtfaiyenin su almak için kullandığı sokak musluğu.", "Yangın musluğu"),
            new ObjectClassInfo("Dur levhası", Trafik, 0.75, 0.75, "Sürücülere tam duruş yapmalarını bildiren trafik işareti.", "Dur işareti"),
            new ObjectClassInfo("Parkmetre", Trafik, 1.30, 0.30, "Park ücreti ödemek için kullanılan sokak cihazı.", "Parkmetre"),
            new ObjectClassInfo("Bank", Mobilya, 0.85, 1.50, "Parklarda ve sokaklarda oturmak için kullanılan uzun oturak.", "Bank"),
            new ObjectClassInfo("Kuş", Hayvan, 0.20, 0.25, "Tüylü, kanatlı ve gagalı omurgalı hayvan.", "Kuşlar"),
            new ObjectClassInfo("Kedi", Hayvan, 0.25, 0.45, "Evcil, küçük etçil memeli.", "Kedi"),
            new ObjectClassInfo("Köpek", Hayvan, 0.55, 0.80, "İnsanın en eski evcil hayvanlarından biri.", "Köpek"),
            new ObjectClassInfo("At", Hayvan, 1.60, 2.20, "Binek ve yük hayvanı olarak kullanılan büyük memeli.", "At"),
            new ObjectClassInfo("Koyun", Hayvan, 0.90, 1.20, "Yünü, eti ve sütü için yetiştirilen çiftlik hayvanı.", "Koyun"),
            new ObjectClassInfo("İnek", Hayvan, 1.40, 2.20, "Sütü ve eti için yetiştirilen büyükbaş hayvan.", "Sığır"),
            new ObjectClassInfo("Fil", Hayvan, 3.00, 5.50, "Hortumu ve dişleriyle tanınan, karadaki en büyük memeli.", "Fil"),
            new ObjectClassInfo("Ayı", Hayvan, 1.20, 1.80, "İri yapılı, güçlü, çoğunlukla hepçil memeli.", "Ayı"),
            new ObjectClassInfo("Zebra", Hayvan, 1.40, 2.30, "Siyah-beyaz çizgili Afrika atgillerinden.", "Zebra"),
            new ObjectClassInfo("Zürafa", Hayvan, 5.00, 3.00, "Boynu en uzun kara hayvanı.", "Zürafa"),
            new ObjectClassInfo("Sırt çantası", Esya, 0.45, 0.30, "Sırtta taşınan, iki askılı çanta.", "Sırt çantası"),
            new ObjectClassInfo("Şemsiye", Esya, 0.90, 1.00, "Yağmur ve güneşten korunmak için açılan katlanır araç.", "Şemsiye"),
            new ObjectClassInfo("El çantası", Esya, 0.30, 0.35, "Elde ya da omuzda taşınan küçük çanta.", "El çantası"),
            new ObjectClassInfo("Kravat", Esya, 0.50, 0.09, "Gömlek yakasına bağlanan uzun kumaş aksesuar.", "Kravat"),
            new ObjectClassInfo("Bavul", Esya, 0.65, 0.45, "Yolculukta eşya taşımak için kullanılan büyük çanta.", "Bavul"),
            new ObjectClassInfo("Frizbi", Spor, 0.03, 0.27, "Atılıp tutularak oynanan uçan disk.", "Frizbi"),
            new ObjectClassInfo("Kayak", Spor, 1.70, 0.10, "Karda kaymak için kullanılan uzun ve dar tahta.", "Kayak"),
            new ObjectClassInfo("Snowboard", Spor, 1.50, 0.30, "Karda kaymak için kullanılan tek geniş tahta.", "Snowboard"),
            new ObjectClassInfo("Top", Spor, 0.22, 0.22, "Çeşitli sporlarda kullanılan küresel oyun aracı.", "Top"),
            new ObjectClassInfo("Uçurtma", Spor, 0.90, 0.80, "İpe bağlanarak rüzgârda uçurulan hafif çerçeve.", "Uçurtma"),
            new ObjectClassInfo("Beyzbol sopası", Spor, 0.85, 0.07, "Beyzbolda topa vurmak için kullanılan sopa.", "Beyzbol sopası"),
            new ObjectClassInfo("Beyzbol eldiveni", Spor, 0.30, 0.25, "Beyzbolda topu yakalamak için kullanılan deri eldiven.", "Beyzbol eldiveni"),
            new ObjectClassInfo("Kaykay", Spor, 0.12, 0.80, "Dört tekerlekli, üzerinde durularak kayılan tahta.", "Kaykay"),
            new ObjectClassInfo("Sörf tahtası", Spor, 2.00, 0.55, "Dalgalar üzerinde kaymak için kullanılan uzun tahta.", "Sörf tahtası"),
            new ObjectClassInfo("Tenis raketi", Spor, 0.68, 0.27, "Teniste topa vurmak için kullanılan file gerili raket.", "Tenis raketi"),
            new ObjectClassInfo("Şişe", Mutfak, 0.25, 0.08, "Sıvıları saklamak için kullanılan dar ağızlı kap.", "Şişe"),
            new ObjectClassInfo("Şarap kadehi", Mutfak, 0.20, 0.08, "İnce saplı içecek bardağı.", "Kadeh"),
            new ObjectClassInfo("Fincan", Mutfak, 0.10, 0.09, "Sıcak içecekler için kulplu küçük kap.", "Fincan"),
            new ObjectClassInfo("Çatal", Mutfak, 0.19, 0.025, "Yemeği tutup ağza götürmek için dişli sofra aleti.", "Çatal"),
            new ObjectClassInfo("Bıçak", Mutfak, 0.22, 0.025, "Kesmek için kullanılan keskin ağızlı alet.", "Bıçak"),
            new ObjectClassInfo("Kaşık", Mutfak, 0.18, 0.04, "Sıvı ve yumuşak yiyecekler için çukur ağızlı sofra aleti.", "Kaşık"),
            new ObjectClassInfo("Kase", Mutfak, 0.08, 0.16, "Çorba, salata gibi yiyecekler için derin kap.", "Kase"),
            new ObjectClassInfo("Muz", Yiyecek, 0.04, 0.19, "Uzun, sarı kabuklu, tatlı tropikal meyve.", "Muz"),
            new ObjectClassInfo("Elma", Yiyecek, 0.08, 0.08, "Dünyada en çok yetiştirilen meyvelerden biri.", "Elma"),
            new ObjectClassInfo("Sandviç", Yiyecek, 0.06, 0.14, "İki dilim ekmek arasına konan yiyecek.", "Sandviç"),
            new ObjectClassInfo("Portakal", Yiyecek, 0.08, 0.08, "C vitamini yönünden zengin turunçgil meyvesi.", "Portakal"),
            new ObjectClassInfo("Brokoli", Yiyecek, 0.15, 0.15, "Lahanagillerden, yeşil çiçek başlı sebze.", "Brokoli"),
            new ObjectClassInfo("Havuç", Yiyecek, 0.03, 0.18, "Turuncu renkli, beta karoten içeren kök sebze.", "Havuç"),
            new ObjectClassInfo("Sosisli sandviç", Yiyecek, 0.05, 0.18, "Uzun ekmek arasına sosis konarak yapılan yiyecek.", "Sosisli sandviç"),
            new ObjectClassInfo("Pizza", Yiyecek, 0.03, 0.30, "İtalyan mutfağından, hamur üzerine malzeme konarak pişirilen yemek.", "Pizza"),
            new ObjectClassInfo("Donut", Yiyecek, 0.04, 0.09, "Halka şeklinde, kızartılmış tatlı hamur.", "Donut"),
            new ObjectClassInfo("Pasta", Yiyecek, 0.12, 0.22, "Kutlamalarda yenen kremalı tatlı.", "Pasta"),
            new ObjectClassInfo("Sandalye", Mobilya, 0.90, 0.45, "Tek kişilik, arkalıklı oturma mobilyası.", "Sandalye"),
            new ObjectClassInfo("Kanepe", Mobilya, 0.85, 2.00, "Birden çok kişinin oturabileceği döşemeli koltuk.", "Kanepe"),
            new ObjectClassInfo("Saksı bitkisi", Ev, 0.45, 0.35, "Saksıda yetiştirilen süs bitkisi.", "Saksı"),
            new ObjectClassInfo("Yatak", Mobilya, 0.55, 1.60, "Üzerinde uyunan mobilya.", "Yatak"),
            new ObjectClassInfo("Yemek masası", Mobilya, 0.75, 1.40, "Yemek yemek için kullanılan masa.", "Masa"),
            new ObjectClassInfo("Klozet", Ev, 0.75, 0.40, "Tuvalette kullanılan oturaklı sıhhi tesisat.", "Klozet"),
            new ObjectClassInfo("Televizyon / Monitör", Elektronik, 0.45, 0.75, "Görüntü göstermek için kullanılan ekran.", "Televizyon"),
            new ObjectClassInfo("Dizüstü bilgisayar", Elektronik, 0.23, 0.34, "Taşınabilir kişisel bilgisayar.", "Dizüstü bilgisayar"),
            new ObjectClassInfo("Fare", Elektronik, 0.04, 0.065, "Bilgisayarda imleci yönetmek için kullanılan giriş aygıtı.", "Fare (bilgisayar)"),
            new ObjectClassInfo("Kumanda", Elektronik, 0.18, 0.05, "Cihazları uzaktan kontrol etmeye yarayan alet.", "Uzaktan kumanda"),
            new ObjectClassInfo("Klavye", Elektronik, 0.04, 0.44, "Bilgisayara yazı girmek için kullanılan tuş takımı.", "Klavye (bilgisayar)"),
            new ObjectClassInfo("Cep telefonu", Elektronik, 0.15, 0.075, "Taşınabilir telefon; çoğunlukla akıllı telefon.", "Cep telefonu"),
            new ObjectClassInfo("Mikrodalga fırın", EvAleti, 0.30, 0.50, "Yiyecekleri mikrodalga ile ısıtan mutfak aleti.", "Mikrodalga fırın"),
            new ObjectClassInfo("Fırın", EvAleti, 0.85, 0.60, "Yemek pişirmek için kullanılan kapalı ısıtma aleti.", "Fırın"),
            new ObjectClassInfo("Ekmek kızartma makinesi", EvAleti, 0.20, 0.28, "Ekmek dilimlerini kızartan elektrikli alet.", "Ekmek kızartma makinesi"),
            new ObjectClassInfo("Lavabo", Ev, 0.20, 0.55, "El yıkamak için kullanılan musluklu tekne.", "Lavabo"),
            new ObjectClassInfo("Buzdolabı", EvAleti, 1.80, 0.70, "Yiyecekleri soğuk tutarak saklayan ev aleti.", "Buzdolabı"),
            new ObjectClassInfo("Kitap", Ev, 0.24, 0.16, "Ciltlenmiş basılı sayfalardan oluşan yayın.", "Kitap"),
            new ObjectClassInfo("Saat", Ev, 0.30, 0.30, "Zamanı gösteren alet.", "Saat"),
            new ObjectClassInfo("Vazo", Ev, 0.30, 0.15, "Çiçek koymak için kullanılan süs kabı.", "Vazo"),
            new ObjectClassInfo("Makas", Ev, 0.18, 0.08, "İki bıçaklı kesme aleti.", "Makas"),
            new ObjectClassInfo("Oyuncak ayı", Ev, 0.35, 0.25, "Ayı biçiminde peluş oyuncak.", "Oyuncak ayı"),
            new ObjectClassInfo("Saç kurutma makinesi", EvAleti, 0.25, 0.25, "Sıcak hava üfleyerek saçı kurutan alet.", "Saç kurutma makinesi"),
            new ObjectClassInfo("Diş fırçası", Ev, 0.19, 0.02, "Dişleri temizlemek için kullanılan küçük fırça.", "Diş fırçası"),
        };

        public static ObjectClassInfo Get(int classId)
        {
            return classId >= 0 && classId < Classes.Length
                ? Classes[classId]
                : new ObjectClassInfo("Bilinmeyen", "-", 0.5, 0.5, "", "");
        }
    }
}
