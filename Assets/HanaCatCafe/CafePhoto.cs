using System;
using System.IO;
using UnityEngine;

namespace Hana.CatCafe
{
    public static class CafePhoto
    {
        // Add a frame outside the photograph: never crop out a cat or dessert.
        public static Texture2D Frame(Texture2D source,int style)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            if(style<0 || style>3)throw new ArgumentOutOfRangeException(nameof(style));
            float scale=Mathf.Min(1,1536f/Mathf.Max(source.width,source.height));
            int w=Mathf.Max(1,Mathf.RoundToInt(source.width*scale)),h=Mathf.Max(1,Mathf.RoundToInt(source.height*scale));
            int shortEdge=Mathf.Min(w,h),pad=Mathf.Max(12,Mathf.RoundToInt(shortEdge*.065f));
            int bottom=style==0 ? pad*3 : pad;
            int width=w+pad*2,height=h+pad+bottom;
            Color32 paper=CafeShapes.Hex(style==1 ? "#ebd6db" : style==2 ? "#dce3d3" : "#f4ead8");
            Color32 accent=CafeShapes.Hex(style==3 ? "#ba9c68" : style==1 ? "#c798a9" : "#a7b69c");
            var pixels=new Color32[width*height];
            int check=Mathf.Max(5,pad/3);
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                Color32 c=paper;
                if(style==2 && ((x/check)%2==0 || (y/check)%2==0))
                    c=Color32.Lerp(paper,accent,(x/check)%2==0 && (y/check)%2==0 ? .32f : .16f);
                pixels[y*width+x]=c;
            }
            // Bilinear resize only for very large Game views; never upscale the photo.
            var input=source.GetPixels32();
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float sx=Mathf.Clamp((x+.5f)*source.width/w-.5f,0,source.width-1);
                float sy=Mathf.Clamp((y+.5f)*source.height/h-.5f,0,source.height-1);
                int x0=(int)sx,y0=(int)sy,x1=Mathf.Min(x0+1,source.width-1),y1=Mathf.Min(y0+1,source.height-1);
                var a=Color32.Lerp(input[y0*source.width+x0],input[y0*source.width+x1],sx-x0);
                var b=Color32.Lerp(input[y1*source.width+x0],input[y1*source.width+x1],sx-x0);
                pixels[(y+bottom)*width+x+pad]=Color32.Lerp(a,b,sy-y0);
            }
            int inset=Mathf.Max(3,pad/3),stroke=Mathf.Max(1,pad/16);
            Border(pixels,width,height,inset,inset,width-inset*2,height-inset*2,stroke,accent);
            if(style==1 || style==3)
            {
                Border(pixels,width,height,inset+4,inset+4,width-inset*2-8,height-inset*2-8,stroke,accent);
                int r=Mathf.Max(2,pad/8),spacing=Mathf.Max(10,pad/2);
                for(int x=pad;x<width-pad;x+=spacing)
                {Dot(pixels,width,height,x,pad/2,r,accent);Dot(pixels,width,height,x,height-pad/2,r,accent);}
                for(int y=pad;y<height-pad;y+=spacing)
                {Dot(pixels,width,height,pad/2,y,r,accent);Dot(pixels,width,height,width-pad/2,y,r,accent);}
            }
            if(style==0)
            {
                // Small paw stamp on the postcard's lower margin, without text.
                int cx=width/2,cy=bottom/2,r=Mathf.Max(2,pad/6);
                Dot(pixels,width,height,cx,cy-r,r*2,accent);
                for(int j=-1;j<=1;j++)Dot(pixels,width,height,cx+j*r*3,cy+r*3,r,accent);
            }
            var result=new Texture2D(width,height,TextureFormat.RGBA32,false);
            result.name="Cafe framed photograph";result.filterMode=FilterMode.Bilinear;
            result.SetPixels32(pixels);result.Apply();return result;
        }
        static void Border(Color32[] p,int w,int h,int x,int y,int bw,int bh,int stroke,Color32 c)
        {
            for(int k=0;k<stroke;k++)
            {
                for(int i=x;i<x+bw;i++){Put(p,w,h,i,y+k,c);Put(p,w,h,i,y+bh-1-k,c);}
                for(int j=y;j<y+bh;j++){Put(p,w,h,x+k,j,c);Put(p,w,h,x+bw-1-k,j,c);}
            }
        }
        static void Dot(Color32[] p,int w,int h,int cx,int cy,int r,Color32 c)
        {
            for(int y=-r;y<=r;y++)for(int x=-r;x<=r;x++)if(x*x+y*y<=r*r)Put(p,w,h,cx+x,cy+y,c);
        }
        static void Put(Color32[] p,int w,int h,int x,int y,Color32 c)
        {if(x>=0&&x<w&&y>=0&&y<h)p[y*w+x]=c;}

        // Returns null on cancellation. Errors are reported to the caller, never as success.
        public static string Save(Texture2D photo)
        {
            string name="Hana-Cafe-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".png";
            string path;
#if UNITY_EDITOR
            path=UnityEditor.EditorUtility.SaveFilePanel("儲存咖啡館照片","",name,"png");
            if(string.IsNullOrEmpty(path))return null;
#elif UNITY_STANDALONE
            string directory=Path.Combine(Application.persistentDataPath,"Photos");
            Directory.CreateDirectory(directory);
            path=Path.Combine(directory,name);
#else
            throw new NotSupportedException("This prototype exports PNG in Unity Editor and desktop builds only.");
#endif
#if UNITY_EDITOR || UNITY_STANDALONE
            byte[] bytes=photo.EncodeToPNG();
            if(bytes==null || bytes.Length==0)throw new IOException("PNG encoding failed.");
            File.WriteAllBytes(path,bytes);return path;
#endif
        }
    }
}
