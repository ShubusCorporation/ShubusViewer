namespace BaseAbstractModel
{
    public abstract class AppAbstractModel
    {
        public abstract void openFile();
        public abstract bool changed { get; set; }
    }
}
