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
// The charger uses a multi-item grid, unlike the existing one-item appliance slots.
(GameItemElement n, GameInventory source, GameItem charger, GameInventory grid, GameItem low, GameItem high) ChargerFixture(){
var charger=new GameItem{id="recharger"};var grid=new GameInventory{owner=charger,capacity=4,mayInventoryAddItemFunc=(i,_)=>i.kind=="battery"&&!i.disposable};
var layout=new Layout();charger.children.Add(layout);layout.children.Add(grid);
var source=new GameInventory();var n=new GameItemElement{kind="battery",charge=0,modifiedShape=new(){x=5}};source.UncheckedAccept(n);
var low=new GameItem{kind="battery",charge=40,modifiedShape=new(){x=0}};var high=new GameItem{kind="battery",charge=90,modifiedShape=new(){x=1}};
grid.UncheckedAccept(low);grid.UncheckedAccept(high);return(n,source,charger,grid,low,high);}
{
var f=ChargerFixture();
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"charger body selects highest charge despite free space");
Check(ChargerTargets.Find(f.n,null,f.grid)==f.high,"open charger grid selects highest");
Check(ChargerTargets.Find(f.n,f.low,null)==f.high,"hovering a battery selects highest in same charger");
f.low.maxCharge=40;f.high.maxCharge=100;
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"remaining energy beats percentage: 90 kJ vs full 40 kJ");
f.high.charge=40;Check(ChargerTargets.Find(f.n,f.charger,null)==f.low,"ties keep stable inventory order");
f.high.charge=95;Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"charge re-evaluated at drop");
Check(SwapLogic.TrySwap(f.n,f.high),"grid swap succeeds");
Check(f.n.parentInventory==f.grid&&f.high.parentInventory==f.source&&f.low.parentInventory==f.grid,"only selected battery exchanged");
Check(f.n.modifiedShape.x==1&&f.high.modifiedShape.x==5,"both batteries occupy vacated positions");
Check(f.n.charge==0&&f.high.charge==95&&f.high.durability==12,"swap preserves charge durability and identity");
Check(f.grid.items.Count==2&&f.source.items.Count==1,"no duplication or loss");
}
{
var f=ChargerFixture();f.grid.capacity=2;
Check(SwapLogic.TrySwap(f.n,ChargerTargets.Find(f.n,f.charger,null)),"full charger swaps without needing an empty cell");
}
foreach(var block in new Action<GameItemElement,GameInventory,GameItem,GameInventory,GameItem,GameItem>[] {
(n,s,c,g,l,h)=>n.charge=100, (n,s,c,g,l,h)=>n.disposable=true,
(n,s,c,g,l,h)=>n.maxCharge=0, (n,s,c,g,l,h)=>n.kind="filter", (n,s,c,g,l,h)=>n.unitCount=2,
(n,s,c,g,l,h)=>n.owned=false, (n,s,c,g,l,h)=>n.removable=false,
(n,s,c,g,l,h)=>c.owned=false,(n,s,c,g,l,h)=>c.id="other_machine",
(n,s,c,g,l,h)=>l.charge=h.charge=0, (n,s,c,g,l,h)=>g.insertLocked=true,
(n,s,c,g,l,h)=>g.removeLocked=true, (n,s,c,g,l,h)=>s.insertLocked=true,
(n,s,c,g,l,h)=>s.removeLocked=true,(n,s,c,g,l,h)=>s.mayInventoryAddItemFunc=(i,_)=>false,
(n,s,c,g,l,h)=>g.mayInventoryAddItemFunc=(i,_)=>false,
(n,s,c,g,l,h)=>{s.Expel(n);g.UncheckedAccept(n);}}){
var f=ChargerFixture();block(f.n,f.source,f.charger,f.grid,f.low,f.high);
Check(ChargerTargets.Find(f.n,f.charger,null)==null,"unsupported/locked charger action keeps native behavior");
}
foreach(var skip in new Action<GameItem>[] {i=>i.removable=false,i=>i.disposable=true,i=>i.owned=false,i=>i.unitCount=2,i=>i.maxCharge=0,i=>i.kind="filter"}){
var f=ChargerFixture();skip(f.high);Check(ChargerTargets.Find(f.n,f.charger,null)==f.low,"highest ineligible battery skipped");
}
{
var f=ChargerFixture();var bag=new GameItem();var bagGrid=new GameInventory{owner=bag};bag.children.Add(bagGrid);bagGrid.UncheckedAccept(f.charger);
Check(ChargerTargets.Find(f.n,bag,null)==null,"dropping on outer bag does not reach nested charger");
var hidden=new GameInventory{owner=f.low};f.low.children.Add(hidden);hidden.UncheckedAccept(new GameItem{kind="battery",charge=1000});
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"does not search inside stored batteries");
}
foreach(var failure in new[]{"source-space","charger-space","return-old","insert-new","throw-insert"}){
var f=ChargerFixture();
if(failure=="source-space"){f.source.capacity=1;f.n.modifiedShape.x=0;f.high.modifiedShape.width=2;}
if(failure=="charger-space"){f.grid.capacity=2;f.n.modifiedShape.width=2;}
if(failure=="return-old")f.source.failAcceptOnce=f.high;
if(failure=="insert-new")f.grid.failAcceptOnce=f.n;
if(failure=="throw-insert")f.grid.throwAcceptOnce=f.n;
var oldSourceX=f.n.modifiedShape.x;
try{Check(!SwapLogic.TrySwap(f.n,f.high),"charger swap rejected: "+failure);}catch(InvalidOperationException){Check(failure=="throw-insert","only expected callback exception");}
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid&&f.low.parentInventory==f.grid,"charger rollback parents: "+failure);
Check(f.n.modifiedShape.x==oldSourceX&&f.high.modifiedShape.x==1,"charger rollback positions: "+failure);
Check(f.grid.items.Count==2&&f.source.items.Count==1&&f.n.charge==0&&f.high.charge==90,"charger rollback conserves items and energy: "+failure);
}
{
var f=ChargerFixture();var appliance=new GameItem();var slot=new GameSlotInventory{owner=appliance,mayInventoryAddItemFunc=(i,_)=>i.kind=="battery"};
f.source.Expel(f.n);slot.UncheckedAccept(f.n);
Check(SwapLogic.TrySwap(f.n,ChargerTargets.Find(f.n,f.charger,null)),"empty battery from appliance can be exchanged");
Check(slot.childItem==f.high&&f.n.parentInventory==f.grid,"charged battery returns to appliance slot");
}
{
var prefix=typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);
var f=ChargerFixture();ModEntry.Enabled=true;object[] callArgs={f.n,true,f.charger,null,1};prefix.Invoke(null,callArgs);
Check(f.n.parentInventory==f.grid&&f.high.parentInventory==f.source,"drop hook chooses charger route");
Check((int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"charger consumes drop to avoid a second native transfer");
f=ChargerFixture();f.source.capacity=1;f.n.modifiedShape.x=0;f.high.modifiedShape.width=2;callArgs=new object[]{f.n,true,null,f.grid,1};prefix.Invoke(null,callArgs);
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid&&(int)callArgs[4]==0&&callArgs[3]==null,"no space cancels charger drop without native follow-up");
foreach(var guard in new[]{"disabled","cancelled","multi","not-dragging","debug"}){
f=ChargerFixture();ModEntry.Enabled=guard!="disabled";f.n.isDebugMenu=guard=="debug";
callArgs=new object[]{f.n,guard!="not-dragging",guard=="cancelled"?null:f.charger,null,guard=="multi"?2:1};prefix.Invoke(null,callArgs);
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid,"charger drag guard: "+guard);
}
}
// Regression: the first charger build rejected every battery above zero charge.
foreach(var charge in new[]{0,1,39,40,89,90,91,100}){
var f=ChargerFixture();f.n.charge=charge;f.grid.capacity=2;
var expected=charge<90?f.high:null;
Check(ChargerTargets.Find(f.n,f.charger,null)==expected,"charge boundary on charger body: "+charge);
Check(ChargerTargets.Find(f.n,null,f.grid)==expected,"charge boundary in charger window: "+charge);
var prefix=typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);
ModEntry.Enabled=true;object[] callArgs={f.n,true,f.charger,null,1};prefix.Invoke(null,callArgs);
if(expected!=null){
Check(f.n.parentInventory==f.grid&&f.high.parentInventory==f.source,"low battery swaps in full charger: "+charge);
Check(f.n.charge==charge&&f.high.charge==90,"low battery swap preserves both charges: "+charge);
Check(callArgs[2]==null&&(int)callArgs[4]==0,"low battery consumes native drop: "+charge);
}else{
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid,"no equal-charge or downgrade auto-swap: "+charge);
Check(callArgs[2]==f.charger&&(int)callArgs[4]==1,"no improvement leaves ordinary insertion available: "+charge);
}
}
{
var f=ChargerFixture();f.n.id="battery_type_2";f.n.charge=12;f.n.modifiedShape.width=2;
f.high.modifiedShape.x=2;f.high.modifiedShape.width=2;f.low.modifiedShape.width=2;
Check(SwapLogic.TrySwap(f.n,ChargerTargets.Find(f.n,f.charger,null)),"partly charged larger battery swaps in full charger");
Check(f.n.modifiedShape.x==2&&f.n.modifiedShape.width==2&&f.high.modifiedShape.x==5,"larger battery keeps correct cells");
}
{
var f=ChargerFixture();f.n.charge=12;ModEntry.Enabled=true;
var hint=typeof(ReplaceHint).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static);
hint.Invoke(null,new object[]{new ItemMouseDragHandler(),f.n,true,f.charger,null,1});
Check(OverlayHandler.current.lastText.Contains("换出电量最多的电池")&&OverlayHandler.current.lastText.Contains("90 kJ"),"low battery hover replaces native cannot-insert hint");
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid,"hover is read only");
}
// Reproduce native append-on-insert ordering, separately from the visible grid positions.
System.Collections.Generic.List<GameItem> Display(GameGridInventory grid){
var raw=new System.Collections.Generic.List<GameItem>(grid.items);
object[] args={grid,raw};
typeof(ChargerDisplayOrder).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
return (System.Collections.Generic.List<GameItem>)args[1];}
foreach(var replacedIndex in new[]{0,1,2,3}){
var charger=new GameItem{id="recharger"};var grid=new GameGridInventory{owner=charger,capacity=4,mayInventoryAddItemFunc=(i,_)=>i.kind=="battery"};
var cells=new GameItem[4];for(int i=0;i<4;i++){cells[i]=new GameItem{kind="battery",charge=i==replacedIndex?90:30,modifiedShape=new(){x=i}};grid.UncheckedAccept(cells[i]);}
var source=new GameInventory();var incoming=new GameItemElement{kind="battery",charge=10};source.UncheckedAccept(incoming);
Check(SwapLogic.TrySwap(incoming,cells[replacedIndex]),"four-cell swap at position "+replacedIndex);
Check(grid.items[3]==incoming,"native append order reproduced");
ModEntry.Enabled=true;var display=Display(grid);
for(int i=0;i<4;i++){
var expected=i==replacedIndex?incoming:cells[i];
Check(display[i]==expected&&expected.modifiedShape.x==i,"casing matches grid position "+i);
}
Check(grid.items[3]==incoming,"display does not mutate native backing list");
Check(incoming.charge==10&&cells[replacedIndex].charge==90&&grid.items.Count==4,"display preserves charge and item count");
Check(SwapLogic.TrySwap(cells[replacedIndex],incoming),"swap back succeeds");
display=Display(grid);for(int i=0;i<4;i++)Check(display[i]==cells[i]&&cells[i].modifiedShape.x==i,"swap back keeps original order");
}
{
var charger=new GameItem{id="recharger"};var grid=new GameGridInventory{owner=charger,capacity=4,mayInventoryAddItemFunc=(i,_)=>true};
GameItem a=new GameItem{modifiedShape=new(){x=0}},b=new GameItem{modifiedShape=new(){x=1}},c=new GameItem{modifiedShape=new(){x=2}};
grid.UncheckedAccept(c);grid.UncheckedAccept(a);grid.UncheckedAccept(b);ModEntry.Enabled=true;
var shown=Display(grid);Check(shown[0]==a&&shown[1]==b&&shown[2]==c,"already mismatched inventories display correctly");
var source=new GameInventory{capacity=1};var incoming=new GameItemElement();source.UncheckedAccept(incoming);b.modifiedShape.width=2;
Check(!SwapLogic.TrySwap(incoming,b),"failed exchange triggers rollback");
shown=Display(grid);Check(shown[0]==a&&shown[1]==b&&shown[2]==c&&b.parentInventory==grid,"rollback display stays aligned");
ModEntry.Enabled=false;shown=Display(grid);for(int i=0;i<grid.items.Count;i++)Check(shown[i]==grid.items[i],"disabled mod preserves native enumeration");
ModEntry.Enabled=true;charger.id="other_machine";shown=Display(grid);for(int i=0;i<grid.items.Count;i++)Check(shown[i]==grid.items[i],"unrelated grid inventories unaffected");
charger.id="recharger";a.modifiedShape.y=1;c.modifiedShape.x=0;
shown=Display(grid);Check(shown[0]==c&&shown[1]==b&&shown[2]==a,"grid view orders rows then columns");
}
Console.WriteLine($"PASS: {checks} checks (game doubles; no live gameplay claim)");
