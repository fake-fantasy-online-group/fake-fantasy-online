using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class EffectPlayer
{
    private const string BeamCBase = "ground-unique:C-base", BeamCCore = "ground-unique:C-core",
        BeamCPool = "ground-unique:C-pool", BeamCThreads = "ground-unique:C-threads", BeamCSpark = "ground-unique:C-spark";
    private static readonly string[] BeamCKeys = { BeamCBase, BeamCCore, BeamCPool, BeamCThreads, BeamCSpark };
    private bool uniqueBeamC;

    internal static void PrewarmUniqueBeamC() { EnsureUniqueBeamC(); }

    private static bool EnsureUniqueBeamC()
    {
        // Corrupt or partially mounted artwork must never break loot receive/update. Staged
        // Unity objects are released by the builder; the owner's existing retry can recover.
        try { return BuildUniqueBeamC(); }
        catch (Exception) { return false; }
    }

    private static bool BuildUniqueBeamC()
    {
        bool ready = true;
        foreach (string key in BeamCKeys)
            ready &= spriteCache.TryGetValue(key, out Sprite[] strip) && strip != null && strip.Length > 0 && strip[0] != null;
        if (!ready)
        {
            byte[] bytes = ReadRes(UniqueGroundBeamResource);
            if (bytes == null) return false;
            DimgDecoder.AnimGeom source = DimgDecoder.ParseAnimGeom(bytes);
            if (source == null || !source.ok || source.f == null || source.parsedFrames < UniqueLootBeamCProfile.Frames) return false;
            int minX = int.MaxValue, minY = int.MaxValue;
            for (int i=0;i<UniqueLootBeamCProfile.Frames;i++)
            { minX=Math.Min(minX,source.f[i].nLeft); minY=Math.Min(minY,source.f[i].nTop); }
            // Source bytes never become temporary Unity textures. Each derived cache entry is
            // immutable, shared by all drops, budgeted, and evicted only when no live effect borrows it.
            var staged = new Dictionary<string,Sprite[]>();
            try
            {
                foreach (string key in BeamCKeys)
                {
                    if(spriteCache.TryGetValue(key,out Sprite[] old) && old != null && old.Length>0 && old[0]!=null) continue;
                    if(key==BeamCThreads)
                    {
                        // The taper and native UV mapping do not animate: evaluate exp/pow once,
                        // not once per native frame. These temporary tables are released after upload.
                        var sampleX=new int[129*541];var sampleY=new int[129*541];var gain=new double[129*541];
                        for(int y=0;y<541;y++)for(int x=0;x<129;x++)
                        {
                            int p=y*129+x;
                            UniqueLootBeamCProfile.SourceSample(x+161,y+65,out sampleX[p],out sampleY[p]);
                            gain[p]=UniqueLootBeamCProfile.Filament(x+161,y+65,1)/.82;
                        }
                        var strip = new Sprite[UniqueLootBeamCProfile.Frames];staged[key]=strip;
                        for(int i=0;i<strip.Length;i++)
                        {
                            DimgDecoder.FrameGeom f=source.f[i];byte[] rgba=DimgDecoder.FrameToRgba(source,i);
                            if(rgba==null)throw new InvalidOperationException("Unique beam source frame has no pixels.");
                            strip[i]=BeamCSprite(161,65,129,541,(x,y)=>
                            {
                                int sample=(int)(y-65)*129+(int)x-161;int sx=sampleX[sample],sy=sampleY[sample];
                                int px=sx+minX-f.nLeft,py=sy+minY-f.nTop;double lum=0;
                                if(px>=0&&py>=0&&px<f.w&&py<f.h)
                                { int p=((f.h-1-py)*f.w+px)*4;lum=Math.Max(rgba[p],Math.Max(rgba[p+1],rgba[p+2]))/255.0*rgba[p+3]/255.0; }
                                double a=Math.Max(lum-.18,0)*gain[sample];return BeamCPixel(a,176,129,56);
                            });
                        }
                    }
                    else if(key==BeamCSpark)
                        staged[key]=new[]{BeamCSprite(219,589,13,33,(x,y)=>BeamCPixel(Math.Exp(-Math.Pow((x-225)/1.2,2)-Math.Pow((y-605)/3.5,2)),223,185,106))};
                    else if(key==BeamCPool)
                        staged[key]=new[]{BeamCSprite(130,565,191,81,(x,y)=>BeamCPixel(UniqueLootBeamCProfile.Pool(x,y),171,118,46))};
                    else
                        staged[key]=new[]{BeamCSprite(130,65,191,541,(x,y)=>
                        {
                            UniqueLootBeamCProfile.Envelope(x,y,out double a,out double correction);
                            return key==BeamCBase?BeamCPixel(a,176,129,56):BeamCPixel(correction,43,53,39);
                        })};
                }
                foreach(var pair in staged){spriteCache[pair.Key]=pair.Value;NoteFx(pair.Key,pair.Value);}
            }
            catch
            {
                foreach(var pair in staged)foreach(Sprite sprite in pair.Value)
                    if(sprite!=null){Destroy(sprite.texture);Destroy(sprite);}
                throw;
            }
        }
        if(!tplCache.ContainsKey(UniqueGroundBeamCacheKey))
        {
            var template = new Template { lifeMs = UniqueLootBeamCProfile.CycleMilliseconds };
            template.layers.Add(BeamCLayer(BeamCBase,true));
            template.layers.Add(BeamCLayer(BeamCCore,false));
            template.layers.Add(BeamCLayer(BeamCPool,false));
            template.layers.Add(BeamCLayer(BeamCThreads,true));
            for(int i=0;i<UniqueLootBeamCProfile.Sparks;i++)
            {
                var layer=new Layer{sprite=BeamCSpark,speed=200f,blendMode=1,cField=-1f};
                var times=new SortedSet<float>();
                for(int frame=0;frame<=40;frame++)times.Add(frame*100f);
                double offset=i*.137-Math.Floor(i*.137);float wrap=(float)((1-offset)*4000);
                if(wrap>0&&wrap<4000){times.Add(wrap-.01f);times.Add(wrap);}
                foreach(float time in times)
                {
                    UniqueLootBeamCProfile.Spark(i,time,out double x,out double y,out double alpha);
                    layer.kfs.Add(new KF{t=time,pos=new Vector3((float)x,0,(float)y),scale=new Vector2(1,1),rgb=new Color(1,1,1,(float)alpha)});
                }
                template.layers.Add(layer);
            }
            tplCache[UniqueGroundBeamCacheKey]=template;
        }
        return true;
    }
    private static Layer BeamCLayer(string key,bool pulse)
    {
        var layer=new Layer{sprite=key,speed=UniqueGroundBeamFrameMilliseconds,blendMode=1,cField=-1f};
        for(int i=0;i<=40;i++)
        {
            float a=pulse?(float)UniqueLootBeamCProfile.Pulse(i*100):1f;
            layer.kfs.Add(new KF{t=i*100,pos=Vector3.zero,scale=new Vector2(1,1),rgb=new Color(a,a,a,1)});
        }
        return layer;
    }
    private static Color32 BeamCPixel(double intensity,int r,int g,int b) => new Color32(
        UniqueLootBeamCProfile.Byte(intensity*r),UniqueLootBeamCProfile.Byte(intensity*g),UniqueLootBeamCProfile.Byte(intensity*b),255);
    private static Sprite BeamCSprite(int left,int top,int width,int height,Func<double,double,Color32> sample)
    {
        var pixels=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[(height-1-y)*width+x]=sample(left+x,top+y);
        Texture2D texture=null;
        try
        {
            texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Unique loot beam C",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,width,height),new Vector2((225f-left)/width,1f-(605f-top)/height),100f,0,SpriteMeshType.FullRect);
        }
        catch{if(texture!=null)Destroy(texture);throw;}
    }
}
}
