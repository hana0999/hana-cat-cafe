using System;
using UnityEditor;
using UnityEngine;

namespace Hana.CatCafe.Editor
{
    public static class CafeLogicChecks
    {
        // Runs in the user's installed Editor without adding a test-framework package.
        [MenuItem("Hana/Run Cafe Logic Checks")]
        public static void Run()
        {
            Vector2 surface;
            Require(!CafeHandsRules.TrySurface(new Vector2(float.NaN,0),.3f,false,out surface),"Invalid direct pointer must not advance a tool");
            Require(!CafeHandsRules.TrySurface(new Vector2(.8f,0),.3f,false,out surface),"Outside pointer must pause rather than paint outside the cake");
            Require(CafeHandsRules.TrySurface(new Vector2(.5f,0),.28f,false,out surface)&&Math.Abs(surface.magnitude-.28f)<.001f,"Near-edge input should clamp to the pastry");
            Require(CafeHandsRules.TrySurface(Vector2.zero,.34f,true,out surface)&&Math.Abs(surface.magnitude-.34f)<.001f,"Donut center input must land on the ring");
            Require(CafeHandsRules.NearTable(new Vector3(-1.45f,2.45f,-2.5f),new Vector3(-1.45f,0,.8f)),"Walking boundary must allow reaching the guest");
            Require(!CafeHandsRules.NearTable(new Vector3(-1.45f,2.45f,-3.3f),new Vector3(-1.45f,0,.8f)),"Distant tray cannot be delivered");
            var flavored=new CafeDrink();flavored.ChooseFlavor(1);flavored.ChooseFlavor(9);
            Require(flavored.Flavor==1,"Invalid flavor must preserve selection");
            flavored.ChooseCup(0);flavored.Tick(1.2f,true);flavored.ChooseFlavor(2);
            Require(flavored.Flavor==1 && Math.Abs(flavored.Fill-.5f)<.0001f,"Flavor must lock while pouring without losing progress");
            flavored.Undo();flavored.Undo();flavored.ChooseFlavor(2);
            Require(flavored.Flavor==2,"Flavor can change after returning to cup stage");
            Vector3 donutOffset;Require(CafeRecipes.TryPlace(CafeRecipes.Donut,Vector2.zero,out donutOffset) && Math.Abs(new Vector2(donutOffset.x+.32f,donutOffset.z).magnitude-.32f)<.001f,"Donut decoration must sit on the ring");
            for(int i=5;i<8;i++)Require(!CafeGuests.Unlocked(i,CafeGuests.All[i].UnlockOrders-1)&&CafeGuests.Unlocked(i,CafeGuests.All[i].UnlockOrders),"New cat unlock boundary");
            var d=new CafeDrink();
            Require(d.Current==CafeDrink.Stage.Cup && !d.Ready,"New drink must be empty");
            d.Tick(100,true);
            Require(d.Fill==0,"Cannot pour before placing a cup");
            d.ChooseCup(-1);d.ChooseCup(3);
            Require(d.Current==CafeDrink.Stage.Cup,"Invalid cup choice must be ignored");
            d.ChooseCup(2);d.ChooseCup(1);
            Require(d.CupColor==2,"Cup selection must lock until undone");
            d.Tick(1.2f,true);
            Require(Math.Abs(d.Fill-.5f)<.0001f,"Half pour must be preserved");
            d.Tick(5,false);d.Tick(-1,true);d.Tick(float.NaN,true);d.Tick(float.PositiveInfinity,true);
            Require(Math.Abs(d.Fill-.5f)<.0001f,"Release/invalid delta must not advance drink");
            d.Tick(100,true);
            Require(d.Current==CafeDrink.Stage.Foam && d.Fill==1 && d.Foam==0,"Long frame must not skip foam");
            d.Tick(.9f,true);d.Undo();
            Require(d.Current==CafeDrink.Stage.Foam && d.Foam==0 && d.Fill==1,"Undo partial foam must keep milk");
            d.Tick(2,true);
            Require(d.Ready && d.Foam==1,"Completed foam must mark drink ready");
            d.Tick(100,true);
            Require(d.Fill==1 && d.Foam==1,"Ready drink must not overflow");
            d.Undo();d.Undo();d.Undo();
            Require(d.Current==CafeDrink.Stage.Cup && d.Fill==0 && d.Foam==0,"Undo must return to empty cup choice");
            d.ChooseCup(0);d.Tick(.5f,true);d.Undo();
            Require(d.Current==CafeDrink.Stage.Pour && d.Fill==0,"Undo partial pour must clear milk first");
            for(int a=0;a<3;a++)for(int b=0;b<3;b++)
                Require(CafeProgress.Matches(a,b,1,2)==(a==1&&b==2),"Order matching must require both ingredients");
            var book=new CafeGuestBook();
            Require(book.Next(0)==0,"First guest must be orange");
            book.Meet(0);
            Require(book.Next(2)==0,"Black cat must stay locked before three orders");
            Require(book.Next(3)==1,"Newly unlocked black cat must visit next");
            book.Meet(1);
            Require(book.Next(5)==0,"Unlocked cats must rotate");
            Require(book.Next(6)==2,"Newly unlocked gray cat must have priority");
            book.Meet(2);
            Require(book.Next(6)==0,"All guests must rotate after first meetings");
            book.entries[0].AddBond(3);
            Require(book.entries[0].Hearts==1 && book.entries[1].bond==0,"Affection must belong to the current cat only");
            book.entries[0].AddBond(-10);book.entries[0].AddBond(100);
            Require(book.entries[0].Hearts==5,"Affection must clamp to five hearts");
            var restored=JsonUtility.FromJson<CafeGuestBook>(JsonUtility.ToJson(book));restored.Normalize();
            Require(restored.entries[0].Hearts==5 && restored.entries[2].met && restored.lastGuest==2,"Guest book must survive serialization");
            restored.entries=new CafeGuestRecord[]{null,new CafeGuestRecord{bond=-9,orders=-2}};
            restored.Normalize();
            Require(restored.entries.Length==CafeGuests.All.Length && restored.entries[0]!=null && restored.entries[1].bond==0 && restored.entries[2]!=null,"Partial saves must normalize safely");
            var decor=new CafeDecor();
            Require(decor.Choose(0,2,0),"Counter colors should be available immediately");
            Require(decor.Choose(1,1,0),"Gingham should be available immediately");
            Require(!decor.Choose(1,2,2) && decor.mat==1,"Locked lace must not change current selection");
            Require(!decor.Choose(2,1,2) && decor.vase==0,"Daisy unlock threshold");
            Require(decor.Choose(1,2,3) && decor.Choose(2,1,3),"Three orders unlock lace and daisies");
            Require(!decor.Choose(2,2,5) && decor.Choose(2,2,6),"Six orders unlock tulips");
            Require(!decor.Choose(-1,0,100) && !decor.Choose(0,3,100),"Invalid decoration IDs must be ignored");
            var restoredDecor=JsonUtility.FromJson<CafeDecor>(JsonUtility.ToJson(decor));restoredDecor.Normalize(6);
            Require(restoredDecor.palette==2 && restoredDecor.mat==2 && restoredDecor.vase==2,"Decor must survive serialization");
            Require(restoredDecor.Choose(2,0,6) && restoredDecor.vase==0 && restoredDecor.mat==2,"Removing flowers must preserve other settings");
            restoredDecor.mat=99;restoredDecor.vase=2;restoredDecor.Normalize(0);
            Require(restoredDecor.mat==0 && restoredDecor.vase==0 && restoredDecor.palette==2,"Corrupt or locked decor must normalize safely");
            var sample=new Texture2D(16,12,TextureFormat.RGBA32,false);
            try
            {
                var samplePixels=new Color32[16*12];
                for(int y=0;y<12;y++)for(int x=0;x<16;x++)samplePixels[y*16+x]=new Color32((byte)(x*12),(byte)(y*15),80,255);
                sample.SetPixels32(samplePixels);sample.Apply();
                for(int style=0;style<4;style++)
                {
                    var framed=CafePhoto.Frame(sample,style);
                    try
                    {
                        int bottom=style==0 ? 36 : 12;
                        Require(framed.width==40 && framed.height==12+12+bottom,"Frame must add borders without cropping");
                        var output=framed.GetPixels32();
                        Require(output[bottom*40+12].Equals(samplePixels[0]),"Photo lower-left corner must be preserved");
                        Require(output[(bottom+11)*40+27].Equals(samplePixels[191]),"Photo upper-right corner must be preserved");
                        byte[] png=framed.EncodeToPNG();
                        Require(png.Length>8&&png[0]==137&&png[1]==80&&png[2]==78&&png[3]==71,"Export must encode PNG");
                    }
                    finally{UnityEngine.Object.DestroyImmediate(framed);}
                }
                Require(sample.width==16&&sample.height==12,"Framing must not resize the source texture");
            }
            finally{UnityEngine.Object.DestroyImmediate(sample);}
            Vector3 placement;
            Require(CafeInteractionRules.TryPlace(Vector2.zero,out placement)&&placement==Vector3.zero,"Center drop must stay centered");
            Require(CafeInteractionRules.TryPlace(new Vector2(.4f,0),out placement),"Near drop must snap onto cream");
            Require(Mathf.Abs(placement.x-.17f)<.0001f&&placement.y<0,"Topping must clamp to cream slope");
            Require(!CafeInteractionRules.TryPlace(new Vector2(.46f,0),out placement),"Outside drop must reject");
            Require(!CafeInteractionRules.TryPlace(new Vector2(float.NaN,0),out placement),"Invalid pointer must reject");
            Require(!CafeInteractionRules.TryPlace(new Vector2(0,float.PositiveInfinity),out placement),"Infinite pointer must reject");
            Require(CafeInteractionRules.TryPlace(new Vector2(-.1f,.1f),out placement)&&placement.x<0&&placement.z>0,"Placement must preserve chosen side");
            Require(CafeInteractionRules.NearGuest(new Vector2(100,0),Vector2.zero),"Near guest drop must accept");
            Require(!CafeInteractionRules.NearGuest(new Vector2(111,0),Vector2.zero),"Far guest drop must reject");
            Require(CafeRecipes.ForOrder(-1)==0 && CafeRecipes.ForOrder(0)==0 && CafeRecipes.ForOrder(1)==1 && CafeRecipes.ForOrder(2)==2 && CafeRecipes.ForOrder(3)==3 && CafeRecipes.ForOrder(4)==4 && CafeRecipes.ForOrder(5)==5 && CafeRecipes.ForOrder(6)==6 && CafeRecipes.ForOrder(7)==0,"Dessert orders must cycle safely");
            Require(CafeRecipes.TryPlace(CafeRecipes.Tart,new Vector2(.5f,0),out placement) && Mathf.Abs(placement.x-.32f)<.0001f && placement.y==0,"Tart placement must clamp to flat filling");
            Require(!CafeRecipes.TryPlace(CafeRecipes.Tart,new Vector2(.61f,0),out placement),"Outside tart must reject");
            Require(CafeRecipes.TryPlace(CafeRecipes.Pudding,new Vector2(.3f,0),out placement) && Mathf.Abs(placement.x-.12f)<.0001f && placement.y==0,"Pudding must keep fruit on its narrow top");
            Require(!CafeRecipes.TryPlace(CafeRecipes.Pudding,new Vector2(.41f,0),out placement),"Outside pudding must reject");
            Require(!CafeRecipes.TryPlace(CafeRecipes.Tart,new Vector2(float.NaN,0),out placement) && !CafeRecipes.TryPlace(CafeRecipes.Pudding,new Vector2(0,float.PositiveInfinity),out placement),"Invalid dessert pointer must reject");
            Require(CafeRecipes.Surface(CafeRecipes.Pudding,true)>CafeRecipes.Surface(CafeRecipes.Pudding,false),"Dollop must raise decoration surface");
            var giftBook=new CafeGuestBook();var gifts=new CafeGifts();
            Require(!gifts.Claim(0,giftBook),"Unmet guest cannot give a gift");
            giftBook.Meet(0);giftBook.entries[0].AddBond(5);
            Require(!gifts.Available(0,giftBook),"Gift must wait for two hearts");
            giftBook.entries[0].AddBond(1);
            Require(gifts.Claim(0,giftBook)&&gifts.Has(0)&&gifts.ribbon,"Two hearts must unlock and equip ribbon");
            Require(!gifts.Claim(0,giftBook),"Gift cannot be claimed twice");
            Require(!gifts.Has(-1)&&!gifts.Claim(3,giftBook),"Invalid gift must reject");
            for(int k=1;k<3;k++){giftBook.Meet(k);giftBook.entries[k].AddBond(6);Require(gifts.Claim(k,giftBook),"Each guest owns an independent gift");}
            var savedGifts=JsonUtility.FromJson<CafeGifts>(JsonUtility.ToJson(gifts));savedGifts.Normalize();
            Require(savedGifts.claimed==7&&savedGifts.ribbon&&savedGifts.flowers,"Claimed gifts must survive serialization");
            savedGifts.claimed=8;savedGifts.Normalize();
            Require(savedGifts.claimed==0&&!savedGifts.ribbon&&!savedGifts.flowers,"Invalid gifts cannot equip locked items");
            var expanded=new CafeGuestBook();expanded.Meet(0);expanded.Meet(1);expanded.Meet(2);
            Require(!CafeGuests.Unlocked(3,8)&&CafeGuests.Unlocked(3,9),"Calico unlocks at nine orders");
            Require(expanded.Next(9)==3,"New calico has priority");expanded.Meet(3);
            Require(!CafeGuests.Unlocked(4,11)&&CafeGuests.Unlocked(4,12)&&expanded.Next(12)==4,"Cream guest unlocks at twelve orders");
            var legacy=JsonUtility.FromJson<CafeGuestBook>("{\"version\":1,\"lastGuest\":2,\"entries\":[{\"met\":true,\"bond\":9,\"orders\":4},{\"met\":true,\"bond\":6},{\"met\":true,\"bond\":3}]}");
            legacy.Normalize();
            Require(legacy.entries.Length==CafeGuests.All.Length&&legacy.entries[0].bond==9&&legacy.entries[1].bond==6&&legacy.entries[2].bond==3&&!legacy.entries[3].met&&!legacy.entries[4].met,"Old three-cat save must retain bonds while appending new cats");
            for(int kind=3;kind<5;kind++)
            {
                Require(CafeRecipes.TryPlace(kind,new Vector2(.5f,0),out placement)&&placement.y==0,"Breakfast decorations sit on a flat surface");
                Require(!CafeRecipes.TryPlace(kind,new Vector2(.61f,0),out placement),"Breakfast outside drop rejects");
                Require(CafeRecipes.Surface(kind,true)>CafeRecipes.Surface(kind,false),"Butter raises topping surface");
            }
            Require(CafeRoomRules.ClampWalk(new Vector3(99,99,-99))==new Vector3(5.2f,2.45f,-9.1f),"Walk must remain inside the clear aisle");
            Require(CafeRoomRules.ClampWalk(new Vector3(float.NaN,0,0))==new Vector3(0,2.45f,-4.4f),"Invalid room position must recover");
            Vector2 look=CafeRoomRules.Look(Vector2.zero,new Vector2(5000,5000));
            Require(look.x>=-180&&look.x<180&&look.y==65,"Look yaw wraps and pitch clamps");
            var bake=new CafeBaking(false);
            bake.Tick(10,false);Require(bake.stage==CafeBaking.Stage.Pour&&bake.amount==0,"Batter requires holding");
            bake.Tick(1,true);Require(Mathf.Abs(bake.amount-.5f)<.0001f,"Partial batter must persist");
            bake.Tick(float.NaN,true);bake.Tick(float.PositiveInfinity,true);bake.Tick(-1,true);
            Require(Mathf.Abs(bake.amount-.5f)<.0001f,"Invalid baking delta cannot corrupt progress");
            bake.Tick(100,true);Require(bake.stage==CafeBaking.Stage.FirstSide&&bake.amount==0,"Long frame cannot skip cooking stages");
            bake.Tick(100,false);Require(bake.stage==CafeBaking.Stage.Turn,"First side must wait for player flip");
            bake.Tick(100,true);Require(bake.stage==CafeBaking.Stage.Turn,"Holding must not auto-flip");
            bake.Turn();bake.Tick(1,false);
            var bakeCopy=JsonUtility.FromJson<CafeBaking>(JsonUtility.ToJson(bake));
            Require(bakeCopy.stage==CafeBaking.Stage.SecondSide&&Mathf.Abs(bakeCopy.amount-.4f)<.0001f,"Baking snapshot must preserve stage and amount");
            bake.Tick(100,false);Require(bake.stage==CafeBaking.Stage.Done,"Second side completes baking");
            bake.Undo();Require(bake.stage==CafeBaking.Stage.Pour&&bake.amount==0,"Rework resets batter");
            var waffleBake=new CafeBaking(true);waffleBake.Turn();
            Require(waffleBake.waffle&&waffleBake.stage==CafeBaking.Stage.Pour,"Waffle lid cannot skip batter");
            var tartPrep=new CafePreparation(false);
            tartPrep.Tick(1,true);tartPrep.Tick(10,false);tartPrep.Tick(float.NaN,true);tartPrep.Tick(float.PositiveInfinity,true);
            Require(tartPrep.stage==CafePreparation.Stage.Press&&Mathf.Abs(tartPrep.amount-.5f)<.0001f,"Dough must pause when released and reject invalid deltas");
            tartPrep.Tick(100,true);Require(tartPrep.stage==CafePreparation.Stage.Done,"Pressed dough enables filling");
            tartPrep.Undo(false);Require(tartPrep.stage==CafePreparation.Stage.Press&&tartPrep.amount==0,"Tart rework clears preparation");
            var puddingPrep=new CafePreparation(true);
            puddingPrep.Unmold();Require(puddingPrep.stage==CafePreparation.Stage.Mix,"Cannot unmold before mixing");
            puddingPrep.Tick(100,true);Require(puddingPrep.stage==CafePreparation.Stage.Pour&&puddingPrep.amount==0,"Mixing cannot skip pouring");
            puddingPrep.Tick(10,false);Require(puddingPrep.amount==0,"Pouring requires holding");
            puddingPrep.Tick(2,true);Require(puddingPrep.stage==CafePreparation.Stage.Set,"Pouring leads to setting");
            puddingPrep.Tick(1,false);
            var prepCopy=JsonUtility.FromJson<CafePreparation>(JsonUtility.ToJson(puddingPrep));
            Require(prepCopy.stage==CafePreparation.Stage.Set&&Mathf.Abs(prepCopy.amount-1f/3)<.0001f,"Preparation state must retain partial progress");
            puddingPrep.Tick(100,false);puddingPrep.Tick(100,true);
            Require(puddingPrep.stage==CafePreparation.Stage.Unmold,"Setting must wait for player's unmold action");
            puddingPrep.Unmold();Require(puddingPrep.stage==CafePreparation.Stage.Done,"Unmolding enables caramel and toppings");
            puddingPrep.Undo(true);Require(puddingPrep.stage==CafePreparation.Stage.Mix,"Pudding rework returns to mixing");
            var shopSettings=new CafeShopSettings{rain=false,dusk=true,leftCushion=true,rightCushion=false};
            var shopCopy=JsonUtility.FromJson<CafeShopSettings>(JsonUtility.ToJson(shopSettings));
            Require(!shopCopy.rain&&shopCopy.dusk&&shopCopy.leftCushion&&!shopCopy.rightCushion,"Independent weather, light and cushion settings must serialize");
            Debug.Log("Cafe logic checks passed. Visuals, pointer handling, camera and serving still require Play-mode testing.");
        }
        static void Require(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException("Cafe check failed: "+message);
        }
    }
}
