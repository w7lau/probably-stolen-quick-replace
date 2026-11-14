using System;
using System.Reflection;
using QuickReplace;

try {
int checks=0;
bool lastDropCancelled=false;
void Drop(MethodInfo method, object[] dropCallData, bool cancelled=false){
    var full=new object[6];Array.Copy(dropCallData,full,5);full[5]=cancelled;
    method.Invoke(null,full);Array.Copy(full,dropCallData,5);lastDropCancelled=(bool)full[5];
}
void Check(bool ok,string message){checks++;if(!ok){Console.Error.WriteLine("FAIL: "+message);Environment.Exit(1);}}
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
var f=Fixture();ModEntry.Enabled=true;var prefix=typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);object[] callArgs={f.n,true,f.device,null,1};Drop(prefix,callArgs);Check(f.n.parentInventory==f.slot&&(int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"native cleanup receives cleared targets after swap");
f=Fixture();callArgs=new object[]{f.n,true,null,null,1};Drop(prefix,callArgs);Original(f,"reset/cancel no replacement");
f=Fixture();f.src.capacity=1;f.o.modifiedShape.width=2;callArgs=new object[]{f.n,true,f.device,null,1};Drop(prefix,callArgs);Original(f,"failed replacement preserves items");Check((int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"failed replacement cancels native follow-up use");
f=Fixture();ModEntry.Enabled=false;callArgs=new object[]{f.n,true,f.device,null,1};Drop(prefix,callArgs);Original(f,"disabled mod no replacement");
}
// The charger uses a multi-item grid, unlike the existing one-item appliance slots.
(GameItemElement n, GameInventory source, GameItem charger, GameInventory grid, GameItem low, GameItem high) ChargerFixture(){
var charger=new GameItem{id="recharger"};var grid=new GameInventory{owner=charger,capacity=2,mayInventoryAddItemFunc=(i,_)=>i.kind=="battery"&&!i.disposable};
var layout=new Layout();charger.children.Add(layout);layout.children.Add(grid);
var source=new GameInventory();var n=new GameItemElement{kind="battery",charge=0,modifiedShape=new(){x=5}};source.UncheckedAccept(n);
var low=new GameItem{kind="battery",charge=40,modifiedShape=new(){x=0}};var high=new GameItem{kind="battery",charge=90,modifiedShape=new(){x=1}};
grid.UncheckedAccept(low);grid.UncheckedAccept(high);return(n,source,charger,grid,low,high);}
{
var f=ChargerFixture();
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"full charger body selects highest charge");
Check(ChargerTargets.Find(f.n,null,f.grid)==null,"open charger grid preserves native placement");
Check(ChargerTargets.Find(f.n,f.low,null)==null,"hovering a battery preserves native targeted exchange");
f.low.maxCharge=40;f.high.maxCharge=100;
Check(ChargerTargets.Find(f.n,f.charger,null)==f.low,"completed 40 kJ battery yields charging space before unfinished 90 kJ battery");
f.low.maxCharge=100;
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
var f=ChargerFixture();ModEntry.Enabled=true;object[] callArgs={f.n,true,f.charger,null,1};Drop(prefix,callArgs);
Check(f.n.parentInventory==f.grid&&f.high.parentInventory==f.source,"drop hook chooses charger route");
Check((int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"charger consumes drop to avoid a second native transfer");
f=ChargerFixture();f.source.capacity=1;f.n.modifiedShape.x=0;f.high.modifiedShape.width=2;callArgs=new object[]{f.n,true,f.charger,null,1};Drop(prefix,callArgs);
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid&&(int)callArgs[4]==0&&callArgs[3]==null,"no space cancels charger drop without native follow-up");
foreach(var guard in new[]{"disabled","cancelled","multi","not-dragging","debug"}){
f=ChargerFixture();ModEntry.Enabled=guard!="disabled";f.n.isDebugMenu=guard=="debug";
callArgs=new object[]{f.n,guard!="not-dragging",guard=="cancelled"?null:f.charger,null,guard=="multi"?2:1};Drop(prefix,callArgs);
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid,"charger drag guard: "+guard);
}
}
// Regression: the first charger build rejected every battery above zero charge.
foreach(var charge in new[]{0,1,39,40,89,90,91,100}){
var f=ChargerFixture();f.n.charge=charge;f.grid.capacity=2;
var expected=charge<90?f.high:null;
Check(ChargerTargets.Find(f.n,f.charger,null)==expected,"charge boundary on charger body: "+charge);
Check(ChargerTargets.Find(f.n,null,f.grid)==null,"charger window never auto-selects: "+charge);
var prefix=typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static);
ModEntry.Enabled=true;object[] callArgs={f.n,true,f.charger,null,1};Drop(prefix,callArgs);
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
var f=ChargerFixture();f.grid.capacity=4;f.n.id="battery_type_2";f.n.charge=12;f.n.modifiedShape.width=2;
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
object[] dropCallData={grid,raw};
typeof(ChargerDisplayOrder).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,dropCallData);
return (System.Collections.Generic.List<GameItem>)dropCallData[1];}
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
// Both a dropper and a bottle satisfy the blender's native liquid-container slot rule.
// The mod must leave the native drop/tooltip arguments untouched for liquid interactions.
foreach(var sourceKind in new[]{"dropper","water_bottle","water_pitcher","future_liquid_container"})
foreach(var volume in new[]{0,10,100})
foreach(var hover in new[]{"bottle","device","slot"}){
var f=Fixture("liquid");f.device.id="blender";f.n.id=sourceKind;
f.n.liquidContainer=true;f.o.liquidContainer=true;f.n.liquidVolume=volume;f.o.liquidVolume=500;
GameItem hovered=hover=="bottle"?f.o:hover=="device"?f.device:null;
GameInventory hoveredInventory=hover=="slot"?f.slot:null;
Check(Targets.Find(f.n,hovered,hoveredInventory)==null,"liquid interaction is not a replacement: "+sourceKind+"/"+volume+"/"+hover);
Check(!SwapLogic.TrySwap(f.n,f.slot),"core swap rejects liquid-for-liquid replacement");
Original(f,"liquid containers stay in their original inventories");
ModEntry.Enabled=true;object[] liquidArgs={f.n,true,hovered,hoveredInventory,1};
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),liquidArgs);
Check(liquidArgs[2]==hovered&&liquidArgs[3]==hoveredInventory&&(int)liquidArgs[4]==1,"native transfer keeps original drop target and count");
OverlayHandler.current.lastText="native pour/draw tooltip";
typeof(ReplaceHint).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new ItemMouseDragHandler(),f.n,true,hovered,hoveredInventory,1});
Check(OverlayHandler.current.lastText=="native pour/draw tooltip","native liquid tooltip not replaced");
Check(f.n.liquidVolume==volume&&f.o.liquidVolume==500,"replacement layer does not alter mixture or liquid volume");
}
{
var f=Fixture("filter");f.device.liquidContainer=true;f.n.id="kotton_fabric";f.o.id="water_filter";
Check(Targets.Find(f.n,f.device,null)==f.slot&&SwapLogic.TrySwap(f.n,f.slot),"cloth replaces pitcher filter despite liquid-container owner");
Check(SwapLogic.TrySwap(f.o,f.slot),"filter can replace cloth in pitcher");
}
// Body drops prefer free space; inside-window drops retain their exact native targets.
foreach(var charge in new[]{0,12,100})
foreach(var capacity in new[]{3,4})
foreach(var hover in new[]{"body","grid","battery"}){
var f=ChargerFixture();f.grid.capacity=capacity;f.n.charge=charge;
f.n.mayTarget=t=>t==f.low||t==f.high; // Native battery-on-battery swap is also available.
var hovered=hover=="body"?f.charger:hover=="battery"?f.low:null;
var inventory=hover=="grid"?f.grid:null;
Check(ChargerTargets.Find(f.n,hovered,inventory)==null,"free charger does not select an exchange: "+hover);
ModEntry.Enabled=true;object[] dropArgs={f.n,true,hovered,inventory,1};
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),dropArgs);
Check(f.n.parentInventory==f.source&&f.high.parentInventory==f.grid&&f.low.parentInventory==f.grid,"insertion is left to native code");
Check((int)dropArgs[4]==1,"free charger preserves insertion count");
Check(dropArgs[2]==hovered&&dropArgs[3]==inventory,"body, battery and grid keep their native targets with free space");
OverlayHandler.current.lastText="native insert";
typeof(ReplaceHint).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new ItemMouseDragHandler(),f.n,true,hovered,inventory,1});
Check(OverlayHandler.current.lastText=="native insert","inside charger preserves native tooltip");
}
{
var f=ChargerFixture();f.grid.capacity=4;f.n.modifiedShape.width=2;f.high.modifiedShape.x=2;f.high.modifiedShape.width=2;
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"one empty cell cannot fit a two-cell battery; exchange remains available");
Check(SwapLogic.TrySwap(f.n,f.high),"large incoming battery uses the space released by the old battery");
}
// Model the game's MayTarget contract for turbo upgrades and other tool interactions.
foreach(var kind in new[]{"turbo_booster_adv","future_upgrade_tool","repair_tool"})
foreach(var canUse in new[]{true,false}){
var f=Fixture("tool");f.device.id="toolbox";f.n.id=kind;f.o.id="turbo_booster_hypertech";
f.n.mayTarget=t=>t==f.o;f.n.canTarget=canUse;
Check(Targets.Find(f.n,f.o,null)==null,"native item use wins over toolbox replacement: "+kind);
object[] dropArgs={f.n,true,f.o,null,1};ModEntry.Enabled=true;
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),dropArgs);
Original(f,"upgrade material and installed tool not exchanged");
Check(dropArgs[2]==f.o&&(int)dropArgs[4]==1,"native upgrade/blocked-use target and count preserved");
OverlayHandler.current.lastText="native upgrade";
typeof(ReplaceHint).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new ItemMouseDragHandler(),f.n,true,f.o,null,1});
Check(OverlayHandler.current.lastText=="native upgrade","native use tooltip preserved");
// Removing the native interaction restores ordinary replacement.
f.n.mayTarget=null;Check(Targets.Find(f.n,f.o,null)==f.slot,"ordinary tool replacement still available");
}
{
var f=Fixture("tool");f.n.mayTarget=t=>t==f.device;
Check(Targets.Find(f.n,f.device,null)==null,"native action on device wins before searching its occupied slots");
var b=Fixture("battery");b.n.mayTarget=t=>t==b.o;
Check(Targets.Find(b.n,b.device,null)==b.slot,"battery dragged onto appliance still replaces its installed battery");
Check(Targets.Find(b.n,b.o,null)==null,"direct battery-on-battery use remains native outside charger");
var c=ChargerFixture();c.n.mayTarget=t=>t==c.low||t==c.high;
Check(ChargerTargets.Find(c.n,c.low,null)==null,"full charger preserves native selected battery swap");
}
// Native gun slots allow player parts on a customer's service gun.
// Cover every standard category, plus each of the three special slots.
foreach(var customerGun in new[]{false,true})
foreach(var mount in new[]{"stock","rail","magazine","underbarrel","barrel","special1","special2","special3"})
foreach(var hover in new[]{"gun","part","slot"}){
var f=Fixture(mount);f.device.id="gun";f.device.owned=!customerGun;f.device.serviceGun=customerGun;
f.n.id="new_"+mount;f.o.id="old_"+mount;f.n.durability=44;f.o.durability=23;
var hovered=hover=="gun"?f.device:hover=="part"?f.o:null;
var inv=hover=="slot"?f.slot:null;
Check(f.n.MayRemove()&&f.o.MayRemove()&&GeneralHelper.MayPlayerInsertInto(f.slot),"native service gun rules permit exchanging player parts");
Check(Targets.Find(f.n,hovered,inv)==f.slot,"gun attachment target: "+customerGun+"/"+mount+"/"+hover);
int callbacks=0,displayedStat=23;f.slot.changed=()=>{callbacks++;displayedStat=f.slot.childItem?.durability??0;};
OverlayHandler.current.lastText="native full slot";ModEntry.Enabled=true;
typeof(ReplaceHint).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new ItemMouseDragHandler(),f.n,true,hovered,inv,1});
Check(OverlayHandler.current.lastText.Contains("换下")&&callbacks==0,"gun replacement hover is read only");
object[] callArgs={f.n,true,hovered,inv,1};
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),callArgs);
Check(f.n.parentInventory==f.slot&&f.o.parentInventory==f.src,"new gun part installed and old part returned");
Check(f.n.durability==44&&f.o.durability==23&&f.device.owned==!customerGun,"part state and gun ownership preserved");
Check(callbacks==2&&displayedStat==44,"native removal/addition callbacks refresh gun stats");
Check((int)callArgs[4]==0&&callArgs[2]==null&&callArgs[3]==null,"gun exchange consumes native follow-up");
}
foreach(var blocked in new[]{"foreign-gun","customer-part","foreign-incoming","locked","nonremovable","incompatible","other-slot-conflict","wrong-weapon"}){
var f=Fixture("rail");f.device.owned=false;f.device.serviceGun=true;
if(blocked=="foreign-gun")f.device.serviceGun=false;
if(blocked=="customer-part")f.o.owned=false;
if(blocked=="foreign-incoming")f.n.owned=false;
if(blocked=="locked")f.slot.insertLocked=true;
if(blocked=="nonremovable")f.o.removable=false;
if(blocked=="incompatible")f.n.kind="barrel";
if(blocked=="other-slot-conflict")f.slot.mayInventoryAddItemFunc=(i,slot)=>false;
if(blocked=="wrong-weapon")f.n.mayInventoryAddItemFunc=(i,slot)=>slot.owner!=f.device;
Check(Targets.Find(f.n,f.device,null)==null&&Targets.Find(f.n,f.o,null)==null&&Targets.Find(f.n,null,f.slot)==null,"native gun restriction preserved: "+blocked);
Check(!SwapLogic.TrySwap(f.n,f.slot),"core swap respects gun restriction: "+blocked);Original(f,"blocked gun swap preserves both items");
}
foreach(var failure in new[]{"no-room","return-old","insert-new","throw-insert"}){
var f=Fixture("stock");f.device.owned=false;f.device.serviceGun=true;
f.n.durability=44;f.o.durability=23;int displayedStat=23;f.slot.changed=()=>displayedStat=f.slot.childItem?.durability??0;
if(failure=="no-room"){f.src.capacity=1;f.o.modifiedShape.width=2;}
if(failure=="return-old")f.src.failAcceptOnce=f.o;
if(failure=="insert-new")f.slot.failAcceptOnce=f.n;
if(failure=="throw-insert")f.slot.throwAcceptOnce=f.n;
try{Check(!SwapLogic.TrySwap(f.n,f.slot),"service gun rollback: "+failure);}catch(InvalidOperationException){Check(failure=="throw-insert","expected callback failure only");}
Original(f,"service gun rollback restores parts");Check(displayedStat==23,"rollback restores native gun stats");
}
{
var f=Fixture("special");f.device.owned=false;f.device.serviceGun=true;
var empty=new GameSlotInventory{owner=f.device,mayInventoryAddItemFunc=(i,_)=>i.kind=="special"};((Layout)f.device.children[0]).children.Add(empty);
Check(Targets.Find(f.n,f.device,null)==null,"empty compatible service-gun slot keeps normal insertion");
Check(Targets.Find(f.n,f.o,null)==f.slot,"explicit old part still identifies which special slot to replace");
f.n.mayTarget=t=>t==f.device;Check(Targets.Find(f.n,f.device,null)==null,"native gun repair/upgrade takes priority");
}
{
var f=Fixture("rail");f.device.owned=false;f.device.serviceGun=true;
var otherGun=new GameItem{serviceGun=true,owned=false};var otherSlot=new GameSlotInventory{owner=otherGun,mayInventoryAddItemFunc=(i,_)=>i.kind=="rail"};
f.src.Expel(f.n);otherSlot.UncheckedAccept(f.n);
Check(SwapLogic.TrySwap(f.n,f.slot),"owned parts can exchange between compatible service-gun slots");
Check(otherSlot.childItem==f.o&&f.slot.childItem==f.n,"cross-gun replacement preserves both attachment positions");
}
// The open charger never auto-selects a different battery, regardless of charge/space.
foreach(var capacity in new[]{2,3})
foreach(var charge in new[]{0,40,100}){
var f=ChargerFixture();f.grid.capacity=capacity;f.n.charge=charge;
f.n.mayTarget=t=>t==f.low||t==f.high;
foreach(var target in new[]{f.low,f.high}){
object[] dropCallData={f.n,true,target,null,1};ModEntry.Enabled=true;
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),dropCallData);
Check(dropCallData[2]==target&&(int)dropCallData[4]==1&&!lastDropCancelled,"manual battery selection is untouched");
Check(f.n.parentInventory==f.source&&f.low.parentInventory==f.grid&&f.high.parentInventory==f.grid,"mod does not preempt native manual exchange");
// Model native MayBatterySwap -> SwapItemsInPlace for the selected item only.
Check(f.n.MayTarget(target)&&SwapLogic.TrySwap(f.n,target),"selected battery can exchange, even when it has less energy");
Check(target.parentInventory==f.source&&f.n.parentInventory==f.grid,"exact selected battery was taken out");
Check(SwapLogic.TrySwap(target,f.n),"reset manual exchange fixture");
}
}
// Hook boundary: exceptions must roll back and return consumed arguments to native cleanup.
foreach(var failure in new[]{"insert-new","source-remove","target-remove","return-old","target-query"}){
var f=Fixture("filter");ModEntry.Enabled=true;
if(failure=="insert-new")f.slot.throwAcceptOnce=f.n;
if(failure=="return-old")f.src.throwAcceptOnce=f.o;
if(failure=="source-remove")f.src.changed=()=>{f.src.changed=null;throw new InvalidOperationException("remove callback");};
if(failure=="target-remove")f.slot.changed=()=>{f.slot.changed=null;throw new InvalidOperationException("remove callback");};
if(failure=="target-query")f.slot.mayInventoryAddItemFunc=(i,_)=>throw new InvalidOperationException("predicate callback");
object[] dropCallData={f.n,true,f.device,null,1};
int errors=UnityEngine.Debug.errors.Count;
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),dropCallData);
Original(f,"hook rollback restores both items after "+failure);
Check((int)dropCallData[4]==0&&dropCallData[2]==null&&dropCallData[3]==null&&lastDropCancelled,"failed hook cancels all native transfer paths");
Check(UnityEngine.Debug.errors.Count==errors+1,"callback failure is logged");
}
{
var f=Fixture();object[] dropCallData={f.n,true,f.device,null,1};ModEntry.Enabled=true;
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),dropCallData,true);
Original(f,"explicit native cancellation must not swap even if stale targets remain");
f=Fixture();dropCallData=new object[]{f.n,true,f.device,null,1};
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),dropCallData);
Check(lastDropCancelled,"successful swap also suppresses machinery-tab fallback");
}
// Full batteries yield their space to incoming batteries that still need charging.
foreach(var scenario in new[]{
 (input:120,max:250,low:100,lowMax:100,high:200,highMax:250,expected:"low"),
 (input:20,max:100,low:100,lowMax:100,high:200,highMax:250,expected:"low"),
 (input:120,max:250,low:90,lowMax:100,high:200,highMax:250,expected:"high"),
 (input:120,max:250,low:90,lowMax:100,high:100,highMax:250,expected:"none"),
 (input:100,max:250,low:100,lowMax:100,high:0,highMax:250,expected:"low"),
 (input:249,max:250,low:100,lowMax:100,high:250,highMax:250,expected:"high"),
 (input:250,max:250,low:100,lowMax:100,high:200,highMax:250,expected:"none"),
 (input:100,max:100,low:100,lowMax:100,high:250,highMax:250,expected:"high"),
 (input:100,max:100,low:100,lowMax:100,high:100,highMax:100,expected:"none"),
 (input:0,max:250,low:100,lowMax:100,high:100,highMax:100,expected:"low")}){
var f=ChargerFixture();f.n.charge=scenario.input;f.n.maxCharge=scenario.max;
f.low.charge=scenario.low;f.low.maxCharge=scenario.lowMax;f.high.charge=scenario.high;f.high.maxCharge=scenario.highMax;
var expected=scenario.expected=="low"?f.low:scenario.expected=="high"?f.high:null;
Check(ChargerTargets.Find(f.n,f.charger,null)==expected,"full priority scenario: "+scenario);
Check(ChargerTargets.Find(f.n,f.low,null)==null&&ChargerTargets.Find(f.n,null,f.grid)==null,"manual targets remain native");
object[] callArgs={f.n,true,f.charger,null,1};ModEntry.Enabled=true;
Drop(typeof(ReplaceOnDrop).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static),callArgs);
if(expected==null){Check(f.n.parentInventory==f.source&&!lastDropCancelled,"no qualifying battery leaves native drop untouched");}
else{
Check(f.n.parentInventory==f.grid&&expected.parentInventory==f.source,"selected full/fallback battery exchanged");
Check(f.n.modifiedShape.x==(expected==f.low?0:1)&&expected.modifiedShape.x==5,"vacated positions preserved");
Check(lastDropCancelled&&(int)callArgs[4]==0,"no second native transfer after full-battery swap");
}
Check(f.n.charge==scenario.input&&f.low.charge==scenario.low&&f.high.charge==scenario.high,"charge and identity preserved");
Check(f.grid.items.Count==2&&f.source.items.Count==1,"items conserved");
}
foreach(var restriction in new[]{"locked","foreign","incompatible","disposable"}){
var f=ChargerFixture();f.n.charge=120;f.n.maxCharge=250;f.low.charge=100;f.low.maxCharge=100;f.high.charge=200;f.high.maxCharge=250;
if(restriction=="locked")f.low.removable=false;
if(restriction=="foreign")f.low.owned=false;
if(restriction=="incompatible")f.source.mayInventoryAddItemFunc=(i,_)=>i!=f.low;
if(restriction=="disposable")f.low.disposable=true;
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"unavailable full battery skipped: "+restriction);
}
{
var f=ChargerFixture();f.n.charge=120;f.n.maxCharge=250;f.low.charge=f.low.maxCharge=100;f.high.charge=200;f.high.maxCharge=250;
f.grid.capacity=3;Check(ChargerTargets.Find(f.n,f.charger,null)==null,"free space still wins over a full battery");
f.grid.capacity=2;
foreach(var locale in new[]{"zh","en"}){
LocHelper.locale=locale;ModEntry.Enabled=true;
typeof(ReplaceHint).GetMethod("Postfix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{new ItemMouseDragHandler(),f.n,true,f.charger,null,1});
Check(OverlayHandler.current.lastText.Contains(locale=="zh"?"已充满":"fully charged"),"localized full-battery hint");
Check(OverlayHandler.current.lastText.Contains("100 kJ"),"hint names the selected battery energy");
}
LocHelper.locale="zh";f.low.charge=99;
Check(ChargerTargets.Find(f.n,f.charger,null)==f.high,"recompute full status at drop after hover");
}
{
var f=ChargerFixture();f.n.charge=120;f.n.maxCharge=250;f.low.charge=f.low.maxCharge=100;
f.source.capacity=1;f.n.modifiedShape.x=0;f.low.modifiedShape.width=2;f.high.modifiedShape.x=2;f.grid.capacity=3;
Check(ChargerTargets.Find(f.n,f.charger,null)==f.low,"full lower-energy battery selected for safe swap attempt");
Check(!SwapLogic.TrySwap(f.n,f.low),"no return space cancels full-battery exchange");
Check(f.n.parentInventory==f.source&&f.low.parentInventory==f.grid&&f.high.parentInventory==f.grid,"no-space rollback preserves both batteries");
Check(f.n.charge==120&&f.low.charge==100&&f.grid.items.Count==2,"rollback preserves charge and count");
}
Console.WriteLine($"PASS: {checks} checks (game doubles; no live gameplay claim)");
} catch(Exception error) {
Console.Error.WriteLine(error);
Environment.Exit(1);
}
