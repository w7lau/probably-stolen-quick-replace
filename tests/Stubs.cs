using System;
using System.Collections.Generic;
using System.Linq;
namespace HarmonyLib { public class HarmonyPatch:Attribute { public HarmonyPatch(Type t,string n){} } }
namespace UnityEngine { public struct Vector2 {} public static class Input { public static Vector2 mousePosition; } }
public interface IMod {}
public class ModManifest {public string ModVersion;}
public class ModLog {public ModLog(ModManifest m){} public void Log(string s){} }
public class RichTextBuilder {public RichTextBuilder(int i){} public RichTextBuilder Clear()=>this;public RichTextBuilder AddLine(string s)=>this;public RichTextBuilder Build(int i=-1)=>this;}
public class OverlayHandler {public static OverlayHandler current=new();public void ShowTooltip(UnityEngine.Vector2 p,UnityEngine.Vector2 t,RichTextBuilder b){} }
public class ItemMouseDragHandler {public UnityEngine.Vector2 tooltipTranslation;}
public interface GraphNodeStorage {List<GraphNodeStorage> children {get;} }
public class Layout:GraphNodeStorage {public List<GraphNodeStorage> children {get;}=new();}
public class GridShape {public int x,width=1; public GridShape Clone()=>new(){x=x,width=width};}
public class GridShapeBuilder {GridShape s; public GridShapeBuilder(GridShape a){s=a.Clone();} public GridShapeBuilder SetTransform(GridShape a){s.x=a.x;return this;}public GridShape Build()=>s;}
public class GameItem:GraphNodeStorage {
 public string name="item",id="",kind="";public bool owned=true,removable=true,destroyed;public int unitCount=1,charge=25,durability=12;
 public GridShape modifiedShape=new(); public GameInventory parentInventory;public List<GraphNodeStorage> children {get;}=new();
 public bool MayRemove()=>parentInventory!=null&&removable&&!parentInventory.removeLocked;public int MaxNumRemove()=>removable?unitCount:0;public void Validate(){} public bool IsDestroyed()=>destroyed;
}
public class GameItemElement:GameItem {public bool isDebugMenu;}
public class GameInventory:GraphNodeStorage {
 public GameItem owner;public int capacity=8;public bool insertLocked,removeLocked;public Func<GameItem,GameInventory,bool> mayInventoryAddItemFunc;
 public GameItem failAcceptOnce,throwAcceptOnce;public List<GameItem> items=new(); public virtual List<GraphNodeStorage> children=>items.Cast<GraphNodeStorage>().ToList();
 public GameItem GetParentItem()=>owner;
 public virtual bool MayHaveValidInventorySlot(GameItem i)=>!insertLocked&&(mayInventoryAddItemFunc?.Invoke(i,this)??true);
 public virtual SlotMarker TryInventorySlot(GameItem i,int n,GridShape shape){if(!MayHaveValidInventorySlot(i)||shape.x<0||shape.x+shape.width>capacity||items.Any(o=>o!=i&&o.modifiedShape.x<shape.x+shape.width&&shape.x<o.modifiedShape.x+o.modifiedShape.width))return null;return new(){item=i,inventory=this,itemGridShape=shape.Clone(),numTransfer=1};}
 public SlotMarker TryFindOneValidInventorySlot(GameItem i){for(int x=0;x<capacity;x++){var s=i.modifiedShape.Clone();s.x=x;var m=TryInventorySlot(i,1,s);if(m!=null)return m;}return null;}
 public virtual bool Expel(GameItem i){if(i.parentInventory!=this)return false;items.Remove(i);i.parentInventory=null;return true;}
 public virtual bool UncheckedAccept(GameItem i){if(i==failAcceptOnce){failAcceptOnce=null;return false;}if(i==throwAcceptOnce){throwAcceptOnce=null;throw new InvalidOperationException("simulated callback");}if(i.parentInventory!=null)return false;items.Add(i);i.parentInventory=this;return true;}
}
public class GameSlotInventory:GameInventory {public GameItem childItem=>items.FirstOrDefault();public override SlotMarker TryInventorySlot(GameItem i,int n,GridShape s)=>childItem!=null?null:base.TryInventorySlot(i,n,new GridShape{width=s.width});public override bool UncheckedAccept(GameItem i)=>childItem==null&&base.UncheckedAccept(i);}
public class SlotMarker {public GameItem item,targetItem;public GameInventory inventory;public GridShape itemGridShape;public int numTransfer;public int AcceptUnchecked(){item.modifiedShape=itemGridShape.Clone();return inventory.UncheckedAccept(item)?item.unitCount:0;} }
public static class GeneralHelper {public static bool IsItemOwned(GameItem i)=>i?.owned==true;public static bool MayPlayerInsertInto(GameInventory i)=>i!=null&&!i.insertLocked&&(i.owner==null||i.owner.owned);}

public static class LocHelper {public static string GetCurrentLocaleCode()=>"zh";}
