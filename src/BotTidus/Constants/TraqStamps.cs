using Stamp = (System.Guid Id, string Name);

namespace BotTidus.Constants
{
    static class TraqStamps
    {
        public static readonly Stamp Clap = (Guid.Parse("027fa329-1b7c-41c3-8b80-665facbdf4aa"), "clap");
        public static readonly Stamp Explosion = (Guid.Parse("27475336-812d-4040-9c0e-c7367cd1c966"), "explosion");
        public static readonly Stamp NoEntrySign = (Guid.Parse("544c04db-9cc3-4c0e-935d-571d4cf103a2"), "no_entry_sign");
        public static readonly Stamp OmaeoKorosu = (Guid.Parse("0190f3ca-05ef-73fa-8892-fe715141f701"), "omaeo_korosu");
        public static readonly Stamp Otsukare = (Guid.Parse("19eb80ae-0467-4409-ad21-5dc5d0148fd6"), "otsukare");
        public static readonly Stamp Question = (Guid.Parse("408b504e-89c1-474b-abfb-16779a3ee595"), "question");
        public static readonly Stamp Wave = (Guid.Parse("54e37bdc-7f8d-4fe9-aaf8-6173b97d0607"), "wave");
        public static readonly Stamp WhiteCheckMark = (Guid.Parse("93d376c3-80c9-4bb2-909b-2bbe2fbf9e93"), "white_check_mark");
    }
}
