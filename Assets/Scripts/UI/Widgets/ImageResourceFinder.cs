namespace UI
{
    public static class ImageResourceFinder
    {
        public static string[] ImagePaths=new string[0];
        public static void SetImage(ResolutionImage image,string name,bool setNativeSize=false)
        {
            if(image==null) return;
            image.SpriteName=name;
            foreach(var path in ImagePaths) { var found=UIBindings.ResolveSprite?.Invoke(path+name); if(found!=null) { image.TexturePath=path; image.sprite=found; break; } }
            if(setNativeSize && image.sprite!=null) image.SetNativeSize();
        }
    }
}
