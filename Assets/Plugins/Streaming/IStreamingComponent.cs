namespace StreamingAssets {
    public interface IStreamingComponent {        
        string Path { get; set; }
        bool IsLoading();
        bool IsLoaded();
        void Prefetch();
        void Unload();
    }
}