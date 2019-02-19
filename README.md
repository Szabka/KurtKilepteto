# KurtKilepteto
KurtKilepteto

nyilvantartas.csv:  
    oszlopok: diakid, kartyaId  
    feladat: Olvasó leolvassa a kártya id-t, majd kikeresi a táblából a hozzá tartozó diák id-t.   
    A diák id-k alapján vannak ugyanitt a fileok, minden diaknak 2 fileja van. az id igazabol relativ utvonal a diakid.txt filehoz, tartalmazhat relativan meghivatkozott konyvtarneveket.

diakid.jpg képfájl / ezeket indulaskor csekkolni kell, hogy jo meretuek -e es amelyik nem azt at kell meretezni.

diakid.txt adatfájl:  
    Elso sor info a diakrol ez megjelenik a kepernyon, felul ide irjak a diak nevet, osztalyt, stb.  
	Masodik sor info a diakrol , nem jelenik meg sehol ide lehet megjegyzeseker irni  
	Harmadik sor kilepesi jogosultsag kezdete ev-honap-nap formaban. Ezzel adott ideig ki is lehet tiltani a kilepest, azon a napon mar mukodik.  
    Tovabbi sorok  
    oszlopok: nap, kezdet-veg  
    minta adatok: H, 8:30-9:15  
    feladat: Adott napon megadott intervallumokban, amikor kimehet. Ezt kell ellenőrizze.  
    Mindenkinek ebédszünet időtartama szerepeljen: H-P 12:15-12:40  
    Nagykorúakat bármikor kiengedheti, tehát: H-P 0:00-23:59...  
    A nap mezo es az idotartam mezo is lehet csillag, igy a nagykoruak bejegyzese: \*,\*  
    tobb szabaly is meg lehet itt adva, mert pl. minden nap kimehet ebedszunetben, de csak kedden es csutortokon van lyukasoraja.    
    
A program ket olvasot kell kezeljen, ini-bol, vagy settingsbol be kell allitani melyik a ki es melyik a be iranyu olvaso.  
A ki iranyu olvasasnal kell legyen egy terulet ami mutogatja az aktualisan kilepo diak kepet az info sorat, es keretet 
ami zold vagy piros annak megfeleloen, hogy talalt-e hozza ervenyes intervallumot.   
Amennyiben az adatbazisban nem szereplo kartyaid-t olvas be az olvaso akkor kep nelkul piros es az info sor helyen ismeretlen kartya: kartyaid szoveg szerepeljen.  
Az ablak egy masik reszen listaszeruen szovegesen listazza az utolso nehany beolvasas adatat, diak txt elso sora, idopont, irany, ervenyes volt-e  
Ezt a belepeseknel is frissiteni kell, es az egeszet naplofileba kell irni folyamatosan, a file neveben legyen benne a datum.

Target OS: Windows 7 


#diakinfo kep felett ket sorban
#ora:perc:ms diak txt elso sora
#eredmeny

#Kep kovetkezo olvasaskor, vagy x masodperc utan automatikusan eltunik
#indulaskor bele teszi a nevebe a datumot de autorotate nincs
#naplofile, ev-ho-nap ora-perc-ms,kartyaid, diak elso sora, esemenytipus, esemenyeredmeny
