namespace ToyProject.Data
{
    public interface IData
    {
        public int Id { get; }
    }

    public class CardSpec : IData
    {
        public int Id { get; set; }
        public string cardName { get; set; }
        public int attack { get; set; }
        public int defense { get; set; }
    }
}