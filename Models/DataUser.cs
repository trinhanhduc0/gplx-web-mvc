namespace DemoGPLX.Models
{
    public class DataUser
    {
        private string _id;
        private string hang;
        private List<int> lsCauSai;
        private List<int> lsCau;
        private List<int> luaChons;
        private List<int> lsCauDiemLiet;
        private List<int> luaChonCauDiemLiet;

        public DataUser()
        {
            luaChons = new List<int>(0);
            lsCau = new List<int>(0);
            lsCauSai = new List<int>(0);
            lsCauDiemLiet = new List<int>(0);
            luaChonCauDiemLiet = new List<int>(0);
            Id = "1";
            hang = "";
        }
        public string Id { get => _id; set => _id = value; }

        public string Hang { get => hang; set => hang = value; }

        public List<int> CauSais { get => lsCauSai; set => lsCauSai = value; }

        public List<int> Caus { get => lsCau; set => lsCau = value; }

        public List<int> LuaChons { get => luaChons; set => luaChons = value; }

        public List<int> CauDiemLiet { get => lsCauDiemLiet; set => lsCauDiemLiet = value; }

        public List<int> LuaChonDiemLiet { get => luaChonCauDiemLiet; set => luaChonCauDiemLiet = value; }

    }
}
