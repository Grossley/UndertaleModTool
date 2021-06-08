FixTexturePages();

string FindTexturePageUsage()
{
    string output = "";
    bool[] isExists = new bool[Data.EmbeddedTextures.Count];
    for (var i = 0; i < isExists.Length; i++)
    {
        isExists[i] = false;
    }
    for (var i = 0; i < Data.TexturePageItems.Count; i++)
    {
        if (Data.TexturePageItems[i].TexturePage != null)
            isExists[(Data.EmbeddedTextures.IndexOf(Data.TexturePageItems[i].TexturePage))] = true;
    }
    for (var i = 0; i < isExists.Length; i++)
    {
        bool bobo = isExists[i];
        output += ("Data.EmbeddedTextures[" + i.ToString() + "].Exists = " + bobo.ToString() + "\r\n");
    }
    return output;
}
string FindNullTexturePages()
{
    string output = "";
    for (var i = 0; i < Data.TexturePageItems.Count; i++)
    {
        if (Data.TexturePageItems[i].TexturePage == null)
            output += (Data.TexturePageItems[i].ToString() + "\r\n");
    }
    return output;
}
void FixNullTexturePages()
{
    for (var i = 0; i < Data.TexturePageItems.Count; i++)
    {
        if (Data.TexturePageItems[i].TexturePage == null)
            Data.TexturePageItems[i].TexturePage = Data.EmbeddedTextures[0];
    }
}
void FixTexturePages()
{
    FixNullTexturePages();
    for (var i = 0; i < Data.TexturePageItems.Count; i++)
    {
        var newIndex = (Data.EmbeddedTextures.IndexOf(Data.TexturePageItems[i].TexturePage) - 1);
        if (newIndex < 0)
            newIndex += Data.EmbeddedTextures.Count;
        Data.TexturePageItems[i].TexturePage = Data.EmbeddedTextures[newIndex];
    }
}