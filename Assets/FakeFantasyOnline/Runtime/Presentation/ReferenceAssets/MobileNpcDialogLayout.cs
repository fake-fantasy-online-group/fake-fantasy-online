using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Presentation-only geometry. Page slots always map to the original option index.
internal sealed class MobileNpcDialogLayout
{
    internal readonly Rect Panel, Close, Body, TextPrevious, TextNext, Previous, Next;
    internal readonly float Scale, RowHeight;
    internal readonly int RowsPerPage, PageCount;
    private readonly float rowTop;
    internal MobileNpcDialogLayout(Rect safe, float scale, int count)
    {
        Scale = scale; RowHeight = 44f * scale;
        float pad = 8f * scale;
        float width = Math.Min(safe.width - 2f * pad, 720f * scale);
        Panel = new Rect(safe.x + (safe.width-width)*.5f, safe.y+pad, width, safe.height-2f*pad);
        Close = new Rect(Panel.xMax-RowHeight, Panel.y, RowHeight, RowHeight);
        float bodyHeight = Math.Max(22f*scale, Math.Min(Panel.height*.32f, 140f*scale));
        Body = new Rect(Panel.x+pad, Close.yMax, Panel.width-2f*pad, bodyHeight);
        TextPrevious = new Rect(Body.x, Body.yMax, RowHeight, RowHeight);
        TextNext = new Rect(Body.xMax-RowHeight, Body.yMax, RowHeight, RowHeight);
        rowTop = TextNext.yMax+pad;
        Previous = new Rect(Body.x, Panel.yMax-pad-RowHeight, RowHeight, RowHeight);
        Next = new Rect(Body.xMax-RowHeight, Previous.y, RowHeight, RowHeight);
        RowsPerPage = Math.Max(1, (int)Math.Floor((Previous.y-rowTop)/RowHeight));
        PageCount = Math.Max(1, (count+RowsPerPage-1)/RowsPerPage);
    }
    internal Rect Row(int slot) => new Rect(Body.x,rowTop+slot*RowHeight,Body.width,RowHeight);

    // Same glyph advance and /Cnn color protocol as DialogUI.DrawWrapped, with
    // color carried across pages. No quest text is discarded or truncated.
    internal static List<string> TextPages(string text, float width, int lines, Func<char,float> advance)
    {
        var pages = new List<string>(); var page = new StringBuilder();
        float x=0; int line=0; string color="";
        text=text??string.Empty; lines=Math.Max(1,lines);
        for(int i=0;i<text.Length;i++)
        {
            char c=text[i];
            if(c=='/' && i+3<text.Length && text[i+1]=='C' && char.IsDigit(text[i+2]) && char.IsDigit(text[i+3]))
            { color=text.Substring(i,4); page.Append(color); i+=3; continue; }
            float w=c=='\n'?0:advance(c);
            if(c=='\n' || x+w>width)
            {
                line++; x=0;
                if(line>=lines) { pages.Add(page.ToString()); page.Clear(); page.Append(color); line=0; }
                else page.Append('\n');
                if(c=='\n') continue;
            }
            page.Append(c); x+=w;
        }
        pages.Add(page.ToString()); return pages;
    }
}
}
