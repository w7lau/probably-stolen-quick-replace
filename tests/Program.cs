using System;
using System.Reflection;
using QuickReplace;

int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
(GameItemElement n,GameItem o,GameSlotInventory slot,GameInventory src,GameItem device) Fixture(string kind="filter"){
var device=new GameItem{id="device"};var src=new GameInventory();var slot=new GameSlotInventory{owner=device,mayInventoryAddItemFunc=(i,_)=>i.kind==kind&&i.id!="uv_filter"};
var layout=new Layout();device.children.Add(layout);layout.children.Add(slot);
var n=new GameItemElement{kind=kind};var o=new GameItem{kind=kind};src.UncheckedAccept(n);slot.UncheckedAccept(o);return(n,o,slot,src,device);}
void Original((GameItemElement n,GameItem o,GameSlotInventory slot,GameInventory src,GameItem device) f,string why){Check(f.n.parentInventory==f.src&&f.o.parentInventory==f.slot&&f.src.items.Count==1&&f.slot.items.Count==1,why);}
foreach(var kind in new[]{"filter","battery","future_module"}){
var f=Fixture(kind);Check(Targets.Find(f.n,f.device,null)==f.slot,"device target "+kind);Check(Targets.Find(f.n,f.o,null)==f.slot,"old item target "+kind);Check(Targets.Find(f.n,null,f.slot)==f.slot,"slot target "+kind);
Check(SwapLogic.TrySwap(f.n,f.slot),"swap "+kind);Check(f.n.parentInventory==f.slot&&f.o.parentInventory==f.src,"parents "+kind);Check(f.o.charge==25&&f.o.durability==12&&f.n.unitCount==1,"preserve item state "+kind);
}
foreach(var setup in new Action<GameItemElement,GameItem,GameSlotInventory,GameInventory,GameItem>[] {
(n,o,s,i,d)=>n.id="uv_filter", (n,o,s,i,d)=>n.kind="wrong", (n,o,s,i,d)=>d.owned=false,
(n,o,s,i,d)=>o.removable=false,(n,o,s,i,d)=>i.removeLocked=true,(n,o,s,i,d)=>s.insertLocked=true,
(n,o,s,i,d)=>n.unitCount=2,(n,o,s,i,d)=>o.unitCount=2,(n,o,s,i,d)=>s.mayInventoryAddItemFunc=null,
(n,o,s,i,d)=>i.mayInventoryAddItemFunc=(x,_)=>x!=o,
(n,o,s,i,d)=>s.mayInventoryAddItemFunc=(x,_)=>false}){
var f=Fixture();setup(f.n,f.o,f.slot,f.src,f.device);Check(!SwapLogic.TrySwap(f.n,f.slot),"blocked native rule");Original(f,"unchanged blocked swap");}
{
var f=Fixture();f.src.capacity=1;f.o.modifiedShape.width=2;Check(!SwapLogic.TrySwap(f.n,f.slot),"old item does not fit");Original(f,"no-space rollback");Check(f.o.modifiedShape.width==2&&f.n.modifiedShape.x==0,"rollback shapes");
}
{
var f=Fixture();f.n.modifiedShape.x=7;f.o.modifiedShape.width=2;Check(SwapLogic.TrySwap(f.n,f.slot),"fallback space in same inventory");Check(f.o.modifiedShape.x==0,"old found alternate position");
}
foreach(var stage in new[]{"return-old","insert-new","throw-insert"}){
var f=Fixture();if(stage=="return-old")f.src.failAcceptOnce=f.o;else if(stage=="insert-new")f.slot.failAcceptOnce=f.n;else f.slot.throwAcceptOnce=f.n;
try{Check(!SwapLogic.TrySwap(f.n,f.slot),"commit failure");}catch(InvalidOperationException){Check(stage=="throw-insert","expected callback exception");}Original(f,"rollback "+stage);
}
{
var f=Fixture();var empty=new GameSlotInventory{owner=f.device,mayInventoryAddItemFunc=(i,_)=>true};((Layout)f.device.children[0]).children.Add(empty);Check(Targets.Find(f.n,f.device,null)==null,"empty slot uses native insert");
}
{
var f=Fixture();var bag=new GameItem();var inv=new GameInventory{owner=bag};bag.children.Add(inv);inv.items.Add(f.device);Check(Targets.Find(f.n,bag,null)==null,"no nested bag replacement");
}
{
var f=Fixture();ModEntry.Enabled=true;var prefix=typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);object[] callArgs={f.n,true,f.device,null,1};prefix.Invoke(null,callArgs);Check(f.n.parentInventory==f.slot&&(int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"native cleanup receives cleared targets after swap");
f=Fixture();callArgs=new object[]{f.n,true,null,null,1};prefix.Invoke(null,callArgs);Original(f,"reset/cancel no replacement");
f=Fixture();f.src.capacity=1;f.o.modifiedShape.width=2;callArgs=new object[]{f.n,true,f.device,null,1};prefix.Invoke(null,callArgs);Original(f,"failed replacement preserves items");Check((int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"failed replacement cancels native follow-up use");
f=Fixture();ModEntry.Enabled=false;callArgs=new object[]{f.n,true,f.device,null,1};prefix.Invoke(null,callArgs);Original(f,"disabled mod no replacement");
}
Console.WriteLine($"PASS: {checks} checks (game doubles; no live gameplay claim)");
