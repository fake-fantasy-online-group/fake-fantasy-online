using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Reviewed open support corridors only. Other gaps of these same assets are not changed.
// Rows are profile-column / support span top / bottom / rear depth in source coordinates.
internal static partial class ReferenceWalkableDeckProfile
{
    internal static bool TryGetMaskDeck(int id, string path, int width, int height, ResLoader.ObjSprite image, out int[] columns)
    {
        columns = null;
        if (image.clipArr != null && image.clipArr.Length != 0) return false;
        if (image.nLeft != 0 || image.nTop != 0) return false;
        int scale = Math.Max(image.hdScale, 1);
        switch (id)
        {
            case 86835501: // jz_cl_0301.gso
                if (width != 681 || height != 868 || image.tex.width != 680 * scale || image.tex.height != 867 * scale ||
                    !string.Equals(path, "object/jz/jz_cl_0301.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor0; return true;
            case 86836101: // jz_cl_0901.gso
                if (width != 551 || height != 554 || image.tex.width != 550 * scale || image.tex.height != 553 * scale ||
                    !string.Equals(path, "object/jz/jz_cl_0901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor1; return true;
            case 422379822: // jz_clmg_0302.gso
                if (width != 289 || height != 180 || image.tex.width != 288 * scale || image.tex.height != 179 * scale ||
                    !string.Equals(path, "object/jz/jz_clmg_0302.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor2; return true;
            case 422380121: // jz_clmg_0601.gso
                if (width != 417 || height != 364 || image.tex.width != 416 * scale || image.tex.height != 363 * scale ||
                    !string.Equals(path, "object/jz/jz_clmg_0601.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor3; return true;
            case 405603005: // jz_dxwg_0701.gso
                if (width != 431 || height != 288 || image.tex.width != 430 * scale || image.tex.height != 287 * scale ||
                    !string.Equals(path, "object/jz/jz_dxwg_0701.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor4; return true;
            case 204276013: // jz_hj_0301.gso
                if (width != 879 || height != 591 || image.tex.width != 878 * scale || image.tex.height != 590 * scale ||
                    !string.Equals(path, "object/jz/jz_hj_0301.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor5; return true;
            case 204277613: // jz_hj_1901.gso
                if (width != 571 || height != 448 || image.tex.width != 570 * scale || image.tex.height != 447 * scale ||
                    !string.Equals(path, "object/jz/jz_hj_1901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor6; return true;
            case 204278813: // jz_hj_3101.gso
                if (width != 751 || height != 564 || image.tex.width != 750 * scale || image.tex.height != 563 * scale ||
                    !string.Equals(path, "object/jz/jz_hj_3101.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor7; return true;
            case 539820233: // jz_hyyhs_0201.gso
                if (width != 541 || height != 508 || image.tex.width != 540 * scale || image.tex.height != 507 * scale ||
                    !string.Equals(path, "object/jz/jz_hyyhs_0201.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor8; return true;
            case 539820234: // jz_hyyhs_0202.gso
                if (width != 543 || height != 508 || image.tex.width != 542 * scale || image.tex.height != 507 * scale ||
                    !string.Equals(path, "object/jz/jz_hyyhs_0202.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor9; return true;
            case 539821033: // jz_hyyhs_1001.gso
                if (width != 401 || height != 322 || image.tex.width != 400 * scale || image.tex.height != 321 * scale ||
                    !string.Equals(path, "object/jz/jz_hyyhs_1001.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor10; return true;
            case 523045318: // jz_hyysc_2502.gso
                if (width != 863 || height != 404 || image.tex.width != 862 * scale || image.tex.height != 403 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2502.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor11; return true;
            case 523045518: // jz_hyysc_2702.gso
                if (width != 377 || height != 589 || image.tex.width != 376 * scale || image.tex.height != 588 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2702.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor12; return true;
            case 523045520: // jz_hyysc_2704.gso
                if (width != 225 || height != 278 || image.tex.width != 224 * scale || image.tex.height != 277 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2704.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor13; return true;
            case 523045617: // jz_hyysc_2801.gso
                if (width != 507 || height != 517 || image.tex.width != 506 * scale || image.tex.height != 516 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2801.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor14; return true;
            case 523045618: // jz_hyysc_2802.gso
                if (width != 293 || height != 449 || image.tex.width != 292 * scale || image.tex.height != 448 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2802.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor15; return true;
            case 523045717: // jz_hyysc_2901.gso
                if (width != 721 || height != 342 || image.tex.width != 720 * scale || image.tex.height != 341 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor16; return true;
            case 523045817: // jz_hyysc_3001.gso
                if (width != 653 || height != 619 || image.tex.width != 652 * scale || image.tex.height != 618 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3001.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor17; return true;
            case 523045818: // jz_hyysc_3002.gso
                if (width != 345 || height != 567 || image.tex.width != 344 * scale || image.tex.height != 566 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3002.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor18; return true;
            case 523045819: // jz_hyysc_3003.gso
                if (width != 691 || height != 565 || image.tex.width != 690 * scale || image.tex.height != 564 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3003.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor19; return true;
            case 523045820: // jz_hyysc_3004.gso
                if (width != 339 || height != 455 || image.tex.width != 338 * scale || image.tex.height != 454 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3004.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor20; return true;
            case 523045917: // jz_hyysc_3101.gso
                if (width != 657 || height != 508 || image.tex.width != 656 * scale || image.tex.height != 507 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3101.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor21; return true;
            case 523045918: // jz_hyysc_3102.gso
                if (width != 381 || height != 429 || image.tex.width != 380 * scale || image.tex.height != 428 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3102.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor22; return true;
            case 523045920: // jz_hyysc_3104.gso
                if (width != 611 || height != 330 || image.tex.width != 610 * scale || image.tex.height != 329 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3104.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor23; return true;
            case 523045921: // jz_hyysc_3105.gso
                if (width != 749 || height != 460 || image.tex.width != 748 * scale || image.tex.height != 459 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3105.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor24; return true;
            case 523046018: // jz_hyysc_3202.gso
                if (width != 487 || height != 415 || image.tex.width != 486 * scale || image.tex.height != 414 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3202.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor25; return true;
            case 523046019: // jz_hyysc_3203.gso
                if (width != 377 || height != 430 || image.tex.width != 376 * scale || image.tex.height != 429 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_3203.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor26; return true;
            case 472711292: // jz_jxmg_0124.gso
                if (width != 565 || height != 507 || image.tex.width != 564 * scale || image.tex.height != 506 * scale ||
                    !string.Equals(path, "object/jz/jz_jxmg_0124.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor27; return true;
            case 472711293: // jz_jxmg_0125.gso
                if (width != 623 || height != 505 || image.tex.width != 622 * scale || image.tex.height != 504 * scale ||
                    !string.Equals(path, "object/jz/jz_jxmg_0125.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor28; return true;
            case 472711294: // jz_jxmg_0126.gso
                if (width != 549 || height != 439 || image.tex.width != 548 * scale || image.tex.height != 438 * scale ||
                    !string.Equals(path, "object/jz/jz_jxmg_0126.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor29; return true;
            case 472711295: // jz_jxmg_0127.gso
                if (width != 467 || height != 437 || image.tex.width != 466 * scale || image.tex.height != 436 * scale ||
                    !string.Equals(path, "object/jz/jz_jxmg_0127.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor30; return true;
            case 472711469: // jz_jxmg_0301.gso
                if (width != 501 || height != 346 || image.tex.width != 500 * scale || image.tex.height != 345 * scale ||
                    !string.Equals(path, "object/jz/jz_jxmg_0301.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor31; return true;
            case 472711470: // jz_jxmg_0302.gso
                if (width != 509 || height != 347 || image.tex.width != 508 * scale || image.tex.height != 346 * scale ||
                    !string.Equals(path, "object/jz/jz_jxmg_0302.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor32; return true;
            case 53280869: // jz_td_0101.gso
                if (width != 669 || height != 555 || image.tex.width != 668 * scale || image.tex.height != 554 * scale ||
                    !string.Equals(path, "object/jz/jz_td_0101.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor33; return true;
            case 53281272: // jz_td_0504.gso
                if (width != 729 || height != 652 || image.tex.width != 728 * scale || image.tex.height != 651 * scale ||
                    !string.Equals(path, "object/jz/jz_td_0504.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor34; return true;
            case 53281569: // jz_td_0801.gso
                if (width != 949 || height != 746 || image.tex.width != 948 * scale || image.tex.height != 745 * scale ||
                    !string.Equals(path, "object/jz/jz_td_0801.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor35; return true;
            case 53283069: // jz_td_2301.gso
                if (width != 643 || height != 553 || image.tex.width != 642 * scale || image.tex.height != 552 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2301.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor36; return true;
            case 53283070: // jz_td_2302.gso
                if (width != 661 || height != 541 || image.tex.width != 660 * scale || image.tex.height != 540 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2302.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor37; return true;
            case 53283269: // jz_td_2501.gso
                if (width != 659 || height != 519 || image.tex.width != 658 * scale || image.tex.height != 518 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2501.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor38; return true;
            case 53283270: // jz_td_2502.gso
                if (width != 675 || height != 530 || image.tex.width != 674 * scale || image.tex.height != 529 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2502.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor39; return true;
            case 53283569: // jz_td_2801.gso
                if (width != 837 || height != 622 || image.tex.width != 836 * scale || image.tex.height != 621 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2801.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor40; return true;
            case 53283669: // jz_td_2901.gso
                if (width != 845 || height != 734 || image.tex.width != 844 * scale || image.tex.height != 733 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor41; return true;
            case 53283769: // jz_td_3001.gso
                if (width != 661 || height != 544 || image.tex.width != 660 * scale || image.tex.height != 543 * scale ||
                    !string.Equals(path, "object/jz/jz_td_3001.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor42; return true;
            case 489488885: // jz_tjfb_0501.gso
                if (width != 463 || height != 302 || image.tex.width != 462 * scale || image.tex.height != 301 * scale ||
                    !string.Equals(path, "object/jz/jz_tjfb_0501.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor43; return true;
            case 455934053: // jz_tjj_0101.gso
                if (width != 745 || height != 531 || image.tex.width != 744 * scale || image.tex.height != 530 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_0101.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor44; return true;
            case 455934153: // jz_tjj_0201.gso
                if (width != 595 || height != 663 || image.tex.width != 594 * scale || image.tex.height != 662 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_0201.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor45; return true;
            case 455934453: // jz_tjj_0501.gso
                if (width != 717 || height != 514 || image.tex.width != 716 * scale || image.tex.height != 513 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_0501.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor46; return true;
            case 455934553: // jz_tjj_0601.gso
                if (width != 609 || height != 510 || image.tex.width != 608 * scale || image.tex.height != 509 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_0601.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor47; return true;
            case 455934853: // jz_tjj_0901.gso
                if (width != 597 || height != 475 || image.tex.width != 596 * scale || image.tex.height != 474 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_0901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor48; return true;
            case 455935054: // jz_tjj_1102.gso
                if (width != 419 || height != 403 || image.tex.width != 418 * scale || image.tex.height != 402 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_1102.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor49; return true;
            case 455935055: // jz_tjj_1103.gso
                if (width != 743 || height != 518 || image.tex.width != 742 * scale || image.tex.height != 517 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_1103.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor50; return true;
            case 455935553: // jz_tjj_1601.gso
                if (width != 769 || height != 577 || image.tex.width != 768 * scale || image.tex.height != 576 * scale ||
                    !string.Equals(path, "object/jz/jz_tjj_1601.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor51; return true;
            case 36506453: // jz_xl_2901.gso
                if (width != 693 || height != 589 || image.tex.width != 692 * scale || image.tex.height != 588 * scale ||
                    !string.Equals(path, "object/jz/jz_xl_2901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor52; return true;
            case 36506553: // jz_xl_3001.gso
                if (width != 899 || height != 678 || image.tex.width != 898 * scale || image.tex.height != 677 * scale ||
                    !string.Equals(path, "object/jz/jz_xl_3001.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor53; return true;
            case 523043817: // jz_hyysc_1001.gso
                if (width != 413 || height != 364 || image.tex.width != 412 * scale || image.tex.height != 363 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1001.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor54; return true;
            case 523044517: // jz_hyysc_1701.gso
                if (width != 187 || height != 226 || image.tex.width != 186 * scale || image.tex.height != 225 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1701.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor55; return true;
            case 523043717: // jz_hyysc_0901.gso
                if (width != 625 || height != 345 || image.tex.width != 624 * scale || image.tex.height != 344 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_0901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor56; return true;
            case 523044117: // jz_hyysc_1301.gso
                if (width != 499 || height != 393 || image.tex.width != 498 * scale || image.tex.height != 392 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1301.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor57; return true;
            case 523044118: // jz_hyysc_1302.gso
                if (width != 499 || height != 393 || image.tex.width != 498 * scale || image.tex.height != 392 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1302.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor58; return true;
            case 523044317: // jz_hyysc_1501.gso
                if (width != 503 || height != 366 || image.tex.width != 502 * scale || image.tex.height != 365 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1501.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor59; return true;
            case 523044318: // jz_hyysc_1502.gso
                if (width != 503 || height != 366 || image.tex.width != 502 * scale || image.tex.height != 365 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1502.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor60; return true;
            case 523044618: // jz_hyysc_1802.gso
                if (width != 403 || height != 305 || image.tex.width != 402 * scale || image.tex.height != 304 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1802.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor61; return true;
            case 523044917: // jz_hyysc_2101.gso
                if (width != 477 || height != 457 || image.tex.width != 476 * scale || image.tex.height != 456 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2101.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor62; return true;
            case 523044918: // jz_hyysc_2102.gso
                if (width != 463 || height != 528 || image.tex.width != 462 * scale || image.tex.height != 527 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2102.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor63; return true;
            case 523045017: // jz_hyysc_2201.gso
                if (width != 523 || height != 544 || image.tex.width != 522 * scale || image.tex.height != 543 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2201.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor64; return true;
            case 523045018: // jz_hyysc_2202.gso
                if (width != 417 || height != 427 || image.tex.width != 416 * scale || image.tex.height != 426 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2202.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor65; return true;
            case 523044417: // jz_hyysc_1601.gso
                if (width != 777 || height != 534 || image.tex.width != 776 * scale || image.tex.height != 533 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1601.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor66; return true;
            case 523044418: // jz_hyysc_1602.gso
                if (width != 777 || height != 534 || image.tex.width != 776 * scale || image.tex.height != 533 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1602.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor67; return true;
            case 523044717: // jz_hyysc_1901.gso
                if (width != 737 || height != 368 || image.tex.width != 736 * scale || image.tex.height != 367 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1901.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor68; return true;
            case 523044817: // jz_hyysc_2001.gso
                if (width != 777 || height != 534 || image.tex.width != 776 * scale || image.tex.height != 533 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2001.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor69; return true;
            case 523044818: // jz_hyysc_2002.gso
                if (width != 777 || height != 534 || image.tex.width != 776 * scale || image.tex.height != 533 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2002.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor70; return true;
            case 523045119: // jz_hyysc_2303.gso
                if (width != 561 || height != 277 || image.tex.width != 560 * scale || image.tex.height != 276 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_2303.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor71; return true;
            case 204278113: // jz_hj_2401.gso
                if (width != 615 || height != 530 || image.tex.width != 614 * scale || image.tex.height != 529 * scale ||
                    !string.Equals(path, "object/jz/jz_hj_2401.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor72; return true;
            case 221053330: // jz_smyw_0402.gso
                if (width != 923 || height != 651 || image.tex.width != 922 * scale || image.tex.height != 650 * scale ||
                    !string.Equals(path, "object/jz/jz_smyw_0402.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor73; return true;
            case 53283369: // jz_td_2601.gso
                if (width != 693 || height != 990 || image.tex.width != 692 * scale || image.tex.height != 989 * scale ||
                    !string.Equals(path, "object/jz/jz_td_2601.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor74; return true;
            case 523043617: // jz_hyysc_0801.gso
                if (width != 385 || height != 269 || image.tex.width != 384 * scale || image.tex.height != 268 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_0801.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor75; return true;
            case 523044617: // jz_hyysc_1801.gso
                if (width != 403 || height != 305 || image.tex.width != 402 * scale || image.tex.height != 304 * scale ||
                    !string.Equals(path, "object/jz/jz_hyysc_1801.gso", StringComparison.OrdinalIgnoreCase)) return false;
                columns = Corridor76; return true;
            default: return false;
        }
    }
    private static readonly int[] Corridor0 = { 18, 0, 460, 380, 19, 0, 440, 360, 20, 0, 420, 360, 21, 0, 390, 360 };
    private static readonly int[] Corridor1 = { 26, 0, 520, 420, 27, 0, 500, 420, 28, 0, 460, 430 };
    private static readonly int[] Corridor2 = { 6, 0, 50, 20, 7, 0, 60, 30, 8, 0, 60, 30, 9, 0, 60, 30, 10, 0, 60, 30, 11, 0, 60, 30, 12, 0, 70, 30, 13, 0, 70, 40, 14, 0, 80, 40, 15, 0, 90, 50, 16, 0, 100, 50, 17, 0, 110, 60, 18, 0, 120, 60, 19, 0, 130, 70, 20, 0, 130, 70, 21, 0, 140, 80 };
    private static readonly int[] Corridor3 = { 19, 0, 320, 290, 20, 0, 320, 250, 21, 0, 330, 250, 22, 0, 330, 250, 23, 0, 330, 250, 24, 0, 330, 290 };
    private static readonly int[] Corridor4 = { 9, 0, 260, 210, 10, 0, 250, 200, 11, 0, 250, 190, 12, 0, 240, 180, 13, 0, 230, 180, 14, 0, 220, 180, 15, 0, 210, 190 };
    private static readonly int[] Corridor5 = { 36, 0, 390, 360, 37, 0, 420, 350, 38, 0, 440, 350, 39, 0, 450, 350 };
    private static readonly int[] Corridor6 = { 29, 0, 330, 310, 30, 0, 350, 310, 31, 0, 370, 320, 32, 0, 390, 330 };
    private static readonly int[] Corridor7 = { 46, 0, 330, 310, 47, 0, 340, 310, 48, 0, 360, 310, 49, 0, 380, 300, 50, 0, 390, 300, 51, 0, 410, 300, 52, 0, 420, 310, 53, 0, 430, 330, 54, 0, 440, 350, 55, 0, 460, 360, 56, 0, 470, 370 };
    private static readonly int[] Corridor8 = { 12, 0, 490, 390, 13, 0, 470, 380, 14, 0, 470, 370, 15, 0, 470, 370, 16, 0, 460, 360, 17, 0, 460, 360, 18, 0, 460, 350, 19, 0, 450, 350, 20, 0, 450, 350, 21, 0, 440, 340, 22, 0, 430, 350, 23, 0, 430, 360, 24, 0, 420, 360, 25, 0, 420, 370, 26, 0, 410, 370, 27, 0, 400, 380 };
    private static readonly int[] Corridor9 = { 27, 0, 410, 380, 28, 0, 420, 370, 29, 0, 430, 370, 30, 0, 430, 360, 31, 0, 440, 360, 32, 0, 440, 350, 33, 0, 450, 350, 34, 0, 450, 340, 35, 0, 450, 340, 36, 0, 460, 340, 37, 0, 460, 350, 38, 0, 470, 350, 39, 0, 470, 360, 40, 0, 470, 360, 41, 0, 480, 360, 42, 0, 480, 370, 43, 0, 490, 370 };
    private static readonly int[] Corridor10 = { 10, 0, 280, 220, 11, 0, 270, 220 };
    private static readonly int[] Corridor11 = { 31, 0, 295, 120, 32, 0, 200, 110, 32, 250, 270, 110, 33, 0, 185, 110, 34, 0, 172, 100, 35, 0, 175, 100, 36, 0, 175, 100, 37, 0, 180, 90, 38, 0, 170, 80, 39, 0, 160, 80, 40, 0, 150, 70, 41, 0, 140, 70, 42, 0, 125, 60, 43, 0, 115, 60, 44, 0, 115, 50, 45, 0, 120, 50, 46, 0, 125, 50, 47, 0, 125, 40, 48, 0, 130, 30 };
    private static readonly int[] Corridor12 = { 2, 0, 289, 190, 3, 0, 305, 190, 4, 0, 316, 190, 5, 0, 323, 190, 6, 0, 329, 190, 7, 0, 334, 190, 8, 0, 337, 190, 9, 0, 338, 190, 10, 0, 340, 190, 11, 0, 341, 190, 12, 0, 342, 190, 13, 0, 342, 190, 14, 0, 342, 190, 15, 0, 342, 190, 16, 0, 342, 190, 17, 0, 340, 190, 18, 0, 338, 190, 19, 0, 336, 190, 20, 0, 333, 190, 21, 0, 330, 190, 22, 0, 327, 190, 23, 0, 324, 190, 24, 0, 321, 190, 25, 0, 300, 190, 26, 0, 300, 190, 27, 0, 300, 190, 28, 0, 298, 190, 29, 0, 290, 190, 30, 0, 335, 170, 31, 0, 335, 170, 32, 0, 333, 160, 33, 0, 330, 160, 34, 0, 300, 160, 35, 0, 298, 160, 36, 0, 298, 150 };
    private static readonly int[] Corridor13 = { 0, 0, 140, 80, 1, 0, 139, 80, 2, 0, 134, 80, 3, 0, 126, 80, 4, 0, 126, 80, 5, 0, 130, 80 };
    private static readonly int[] Corridor14 = { 33, 0, 263, 80, 34, 0, 276, 80, 35, 0, 180, 180, 35, 180, 288, 100, 36, 0, 180, 180, 36, 180, 298, 120, 37, 0, 180, 180, 37, 180, 307, 140, 38, 0, 180, 180, 38, 180, 315, 150, 39, 0, 180, 180, 39, 180, 323, 170, 40, 0, 80, 180, 40, 80, 180, 215, 40, 180, 215, 215, 40, 215, 331, 180, 41, 0, 80, 190, 41, 80, 215, 215, 41, 215, 338, 190, 42, 0, 80, 200, 42, 80, 215, 215, 42, 215, 344, 200, 43, 0, 80, 230, 43, 80, 155, 215, 43, 155, 350, 230, 44, 0, 80, 250, 44, 80, 155, 215, 44, 155, 355, 250, 45, 0, 80, 260, 45, 80, 155, 215, 45, 155, 360, 260, 46, 0, 80, 270, 46, 80, 155, 215, 46, 155, 365, 270, 47, 0, 80, 280, 47, 80, 155, 215, 47, 155, 370, 280, 48, 0, 375, 280, 49, 0, 381, 290, 50, 0, 386, 290 };
    private static readonly int[] Corridor15 = { 0, 0, 210, 100, 1, 0, 210, 110, 2, 0, 210, 110, 3, 0, 220, 110, 4, 0, 220, 120, 5, 0, 230, 120, 6, 0, 230, 120, 7, 0, 230, 130, 8, 0, 240, 130, 9, 0, 250, 130, 10, 0, 250, 140, 11, 0, 260, 140, 12, 0, 260, 150, 13, 0, 270, 150, 14, 0, 280, 160, 15, 0, 290, 160, 16, 0, 290, 160, 17, 0, 290, 170, 18, 0, 300, 170, 19, 0, 310, 170, 20, 0, 310, 180, 21, 0, 320, 180 };
    private static readonly int[] Corridor16 = { 30, 0, 80, 230, 30, 80, 215, 215, 30, 215, 334, 230, 31, 0, 80, 180, 31, 80, 180, 215, 31, 180, 215, 215, 31, 215, 329, 200, 32, 0, 80, 180, 32, 80, 180, 215, 32, 180, 215, 215, 32, 215, 323, 190, 33, 0, 180, 180, 33, 180, 318, 170, 34, 0, 180, 180, 34, 180, 313, 160, 35, 0, 180, 180, 35, 180, 307, 150, 36, 0, 180, 180, 36, 180, 300, 120, 37, 0, 180, 180, 37, 180, 294, 110, 38, 0, 287, 100, 39, 0, 279, 90, 40, 0, 270, 90, 41, 0, 259, 90, 42, 0, 246, 90, 43, 0, 231, 90, 44, 0, 211, 90 };
    private static readonly int[] Corridor17 = { 31, 0, 325, 350, 31, 325, 540, 250, 32, 0, 325, 350, 32, 325, 545, 250, 33, 0, 325, 350, 33, 325, 548, 250, 34, 0, 325, 350, 34, 325, 550, 270, 35, 0, 325, 350, 35, 325, 550, 300, 36, 0, 325, 350, 36, 325, 548, 300, 37, 0, 325, 350, 37, 325, 540, 300, 38, 0, 325, 350, 38, 325, 538, 300, 39, 0, 325, 350, 39, 325, 540, 310, 40, 0, 325, 350, 40, 325, 544, 310, 41, 0, 325, 350, 41, 325, 550, 320, 42, 0, 325, 350, 42, 325, 558, 320, 43, 0, 325, 350, 43, 325, 570, 320, 44, 0, 325, 350, 44, 325, 580, 320, 45, 0, 325, 350, 45, 325, 585, 330, 46, 0, 325, 350, 46, 325, 590, 330, 47, 0, 325, 350, 47, 325, 594, 330, 48, 0, 325, 350, 48, 325, 596, 340, 49, 0, 410, 420, 49, 410, 597, 380, 50, 0, 410, 420, 50, 410, 597, 380, 51, 0, 410, 420, 51, 410, 510, 380, 52, 0, 410, 420, 52, 410, 500, 390, 53, 0, 410, 420, 53, 410, 495, 400, 54, 0, 410, 420, 54, 410, 493, 400, 55, 0, 410, 420, 55, 410, 495, 400, 56, 0, 410, 420, 56, 410, 498, 410, 57, 0, 410, 420, 57, 410, 505, 410, 58, 0, 410, 420, 58, 410, 520, 410, 59, 0, 410, 420, 59, 410, 539, 410, 60, 0, 420, 430, 60, 420, 530, 410, 61, 0, 420, 430, 61, 420, 515, 400, 62, 0, 420, 430, 62, 420, 511, 400, 63, 0, 420, 430, 63, 420, 507, 400, 64, 0, 420, 430, 64, 420, 503, 390 };
    private static readonly int[] Corridor18 = { 0, 0, 450, 350, 1, 0, 450, 350, 2, 0, 440, 340, 3, 0, 440, 330, 4, 0, 430, 320, 5, 0, 430, 320, 6, 0, 430, 320, 7, 0, 430, 320, 8, 0, 420, 310, 9, 0, 420, 310, 10, 0, 420, 310, 11, 0, 420, 310, 12, 0, 410, 310, 13, 0, 400, 310, 14, 0, 400, 300, 15, 0, 400, 300, 16, 0, 390, 290, 17, 0, 390, 290, 18, 0, 390, 290, 19, 0, 390, 290, 20, 0, 377, 280, 21, 0, 373, 270 };
    private static readonly int[] Corridor19 = { 45, 0, 153, 0, 46, 0, 164, 0, 47, 0, 174, 0, 48, 0, 183, 0, 49, 0, 191, 30, 50, 0, 199, 40, 51, 0, 206, 40, 52, 0, 125, 125, 52, 125, 213, 60, 53, 0, 125, 125, 53, 125, 219, 70, 54, 0, 125, 125, 54, 125, 224, 80, 55, 0, 125, 125, 55, 125, 228, 90, 56, 0, 125, 125, 56, 125, 233, 100, 57, 0, 125, 125, 57, 125, 237, 120, 58, 0, 242, 130, 59, 0, 246, 140, 60, 0, 250, 150, 61, 0, 254, 160, 62, 0, 90, 170, 62, 90, 185, 190, 62, 185, 258, 170, 63, 0, 90, 170, 63, 90, 185, 190, 63, 185, 262, 170, 64, 0, 90, 170, 64, 90, 185, 190, 64, 185, 266, 170, 65, 0, 90, 190, 65, 90, 185, 190, 65, 185, 270, 190, 66, 0, 90, 190, 66, 90, 185, 190, 66, 185, 275, 190, 67, 0, 90, 190, 67, 90, 185, 190, 67, 185, 279, 190, 68, 0, 90, 190, 68, 90, 185, 190, 68, 185, 284, 190 };
    private static readonly int[] Corridor20 = { 0, 0, 370, 200, 1, 0, 370, 200, 2, 0, 370, 200, 3, 0, 380, 210, 4, 0, 380, 210, 5, 0, 390, 210, 6, 0, 390, 220, 7, 0, 400, 220, 8, 0, 400, 220, 9, 0, 410, 220, 10, 0, 410, 220, 11, 0, 410, 230, 12, 0, 420, 230, 13, 0, 430, 240, 14, 0, 430, 250 };
    private static readonly int[] Corridor21 = { 7, 0, 430, 320, 8, 0, 432, 320, 9, 0, 430, 330, 10, 0, 430, 330, 11, 0, 423, 330, 12, 0, 425, 330, 13, 0, 433, 330, 14, 0, 440, 340, 15, 0, 450, 340, 16, 0, 450, 340, 17, 0, 448, 350, 18, 0, 452, 350, 19, 0, 455, 360, 20, 0, 462, 360, 21, 0, 470, 370, 22, 0, 470, 370, 23, 0, 470, 370, 24, 0, 470, 370, 25, 0, 470, 380, 26, 0, 480, 380 };
    private static readonly int[] Corridor22 = { 8, 0, 400, 330, 9, 0, 405, 330, 10, 0, 408, 330, 11, 0, 410, 330, 12, 0, 410, 330, 13, 0, 403, 320, 14, 0, 380, 320, 15, 0, 375, 320, 16, 0, 355, 330, 17, 0, 350, 340 };
    private static readonly int[] Corridor23 = { 0, 0, 85, 210, 0, 85, 190, 190, 0, 190, 321, 210, 1, 0, 85, 200, 1, 85, 190, 190, 1, 190, 316, 200, 2, 0, 85, 170, 2, 85, 170, 190, 2, 170, 190, 190, 2, 190, 310, 190, 3, 0, 85, 170, 3, 85, 170, 190, 3, 170, 190, 190, 3, 190, 304, 170, 4, 0, 170, 170, 4, 170, 298, 160, 5, 0, 170, 170, 5, 170, 291, 150, 6, 0, 170, 170, 6, 170, 285, 130, 7, 0, 170, 170, 7, 170, 278, 120, 8, 0, 170, 170, 8, 170, 270, 90, 9, 0, 262, 90, 10, 0, 253, 90, 11, 0, 242, 90, 12, 0, 230, 90, 13, 0, 216, 90, 14, 0, 198, 90 };
    private static readonly int[] Corridor24 = { 18, 0, 285, 140, 19, 0, 280, 130, 20, 0, 275, 120, 16, 0, 295, 150, 17, 0, 290, 150, 21, 0, 270, 120, 22, 0, 265, 120, 23, 0, 260, 120, 24, 0, 255, 120, 25, 0, 250, 120, 26, 0, 245, 120, 27, 0, 240, 120, 28, 0, 235, 120, 29, 0, 230, 120, 30, 0, 225, 120, 31, 0, 220, 120, 32, 0, 215, 120, 33, 0, 210, 120, 34, 0, 205, 120 };
    private static readonly int[] Corridor25 = { 22, 0, 110, 40, 23, 0, 111, 40, 24, 0, 114, 40, 25, 0, 119, 40, 26, 0, 126, 40, 27, 0, 145, 40, 28, 0, 151, 40, 29, 0, 160, 40, 30, 0, 160, 40, 31, 0, 160, 40, 32, 0, 160, 50, 33, 0, 160, 50, 34, 0, 160, 50, 35, 0, 160, 60, 36, 0, 170, 60, 37, 0, 144, 70, 38, 0, 139, 70, 39, 0, 139, 70, 40, 0, 141, 70, 41, 0, 149, 70, 42, 0, 155, 70, 43, 0, 165, 80, 44, 0, 181, 80, 45, 0, 210, 90, 46, 0, 210, 90, 47, 0, 220, 90, 48, 0, 230, 80 };
    private static readonly int[] Corridor26 = { 0, 0, 205, 80, 1, 0, 210, 90, 2, 0, 220, 90, 3, 0, 235, 100, 4, 0, 250, 100, 5, 0, 265, 110, 6, 0, 280, 120, 7, 0, 290, 120, 8, 0, 300, 120, 9, 0, 310, 120, 10, 0, 320, 120, 11, 0, 330, 120, 12, 0, 340, 120 };
    private static readonly int[] Corridor27 = { 24, 0, 440, 350, 25, 0, 440, 355, 26, 0, 440, 355, 27, 0, 440, 360, 28, 0, 420, 365, 29, 0, 293, 370, 29, 325, 420, 370 };
    private static readonly int[] Corridor28 = { 40, 0, 420, 390, 41, 0, 420, 390, 42, 0, 430, 380, 43, 0, 430, 370, 44, 0, 450, 360, 45, 0, 450, 360, 46, 0, 450, 360, 47, 0, 460, 350 };
    private static readonly int[] Corridor29 = { 31, 0, 350, 330, 32, 0, 350, 320, 33, 0, 350, 310, 34, 0, 350, 310, 35, 0, 350, 300, 36, 0, 360, 300, 37, 0, 360, 290, 38, 0, 370, 290, 39, 0, 370, 290, 40, 0, 370, 290, 41, 0, 380, 300, 42, 0, 390, 310 };
    private static readonly int[] Corridor30 = { 13, 0, 420, 300, 14, 0, 420, 300, 15, 0, 380, 300, 16, 0, 370, 300, 17, 0, 360, 300, 18, 0, 350, 300, 19, 0, 340, 300 };
    private static readonly int[] Corridor31 = { 13, 0, 170, 100, 14, 0, 168, 110, 15, 0, 180, 110, 16, 0, 185, 110, 17, 0, 190, 120, 18, 0, 195, 120, 19, 0, 200, 130, 20, 0, 205, 130, 21, 0, 210, 130, 22, 0, 205, 130, 23, 0, 199, 140, 24, 0, 195, 150, 24, 208, 225, 150, 25, 0, 230, 150, 26, 0, 235, 150, 27, 0, 240, 160 };
    private static readonly int[] Corridor32 = { 24, 0, 255, 170, 25, 0, 250, 170, 26, 0, 245, 160, 27, 0, 240, 150, 28, 0, 195, 140, 28, 224, 235, 140, 29, 0, 199, 140, 30, 0, 206, 140, 31, 0, 215, 130, 32, 0, 215, 120, 33, 0, 210, 120, 34, 0, 205, 120, 35, 0, 200, 120, 36, 0, 195, 110, 37, 0, 190, 110, 38, 0, 168, 110, 39, 0, 175, 110, 40, 0, 175, 110, 41, 0, 170, 100 };
    private static readonly int[] Corridor33 = { 17, 0, 515, 470, 18, 0, 505, 460, 19, 0, 494, 450, 20, 0, 485, 440, 21, 0, 477, 440, 22, 0, 470, 430, 23, 0, 464, 430, 24, 0, 458, 420, 25, 0, 452, 420, 26, 0, 447, 410, 27, 0, 442, 410, 28, 0, 438, 420, 29, 0, 435, 420, 30, 0, 470, 420, 31, 0, 480, 430, 32, 0, 480, 440, 33, 0, 490, 440, 34, 0, 490, 450, 35, 0, 490, 450, 36, 0, 490, 460, 37, 0, 480, 460, 38, 0, 480, 460 };
    private static readonly int[] Corridor34 = { 57, 0, 580, 570, 58, 0, 590, 560, 59, 0, 600, 560 };
    private static readonly int[] Corridor35 = { 74, 0, 640, 620, 75, 0, 660, 610, 76, 0, 670, 610, 77, 0, 690, 600, 78, 0, 710, 600 };
    private static readonly int[] Corridor36 = { 17, 0, 523, 460, 18, 0, 514, 450, 19, 0, 505, 440, 20, 0, 497, 440, 21, 0, 488, 450 };
    private static readonly int[] Corridor37 = { 53, 0, 479, 430, 54, 0, 491, 430, 55, 0, 502, 430, 56, 0, 513, 440 };
    private static readonly int[] Corridor38 = { 28, 0, 470, 390, 29, 0, 435, 400, 30, 0, 425, 400, 31, 0, 396, 400 };
    private static readonly int[] Corridor39 = { 47, 0, 402, 410, 48, 0, 394, 410, 49, 0, 394, 410, 50, 0, 433, 400, 51, 0, 470, 400 };
    private static readonly int[] Corridor40 = { 29, 0, 578, 490, 30, 0, 583, 480, 31, 0, 571, 480, 32, 0, 557, 470, 33, 0, 545, 470, 34, 0, 536, 460, 35, 0, 532, 460, 36, 0, 527, 450, 37, 0, 522, 450, 38, 0, 517, 440, 39, 0, 512, 440, 40, 0, 507, 440, 41, 0, 502, 440, 42, 0, 497, 450, 43, 0, 493, 450, 44, 0, 489, 460 };
    private static readonly int[] Corridor41 = { 30, 0, 653, 590, 31, 0, 653, 590, 32, 0, 651, 590, 33, 0, 648, 600, 34, 0, 643, 600, 35, 0, 639, 610 };
    private static readonly int[] Corridor42 = { 51, 0, 460, 440, 52, 0, 470, 430, 53, 0, 480, 430 };
    private static readonly int[] Corridor43 = { 13, 0, 272, 200, 14, 0, 264, 180, 15, 0, 256, 190, 16, 0, 248, 200, 17, 0, 240, 200, 18, 0, 232, 210 };
    private static readonly int[] Corridor44 = { 53, 0, 450, 410, 54, 0, 480, 400 };
    private static readonly int[] Corridor45 = { 33, 0, 490, 290, 34, 290, 350, 290, 34, 382, 460, 290, 35, 290, 339, 290, 35, 390, 440, 290 };
    private static readonly int[] Corridor46 = { 36, 0, 513, 440, 37, 0, 513, 440, 38, 0, 513, 440, 39, 0, 513, 440 };
    private static readonly int[] Corridor47 = { 27, 0, 460, 400, 28, 0, 450, 400, 29, 0, 430, 410 };
    private static readonly int[] Corridor48 = { 41, 0, 365, 340, 42, 0, 375, 330, 43, 0, 392, 330, 44, 0, 415, 320 };
    private static readonly int[] Corridor49 = { 17, 0, 150, 130, 18, 0, 160, 120, 19, 0, 160, 110, 20, 0, 170, 110, 21, 0, 170, 100, 22, 0, 170, 100, 23, 0, 180, 100, 24, 0, 180, 100, 25, 0, 190, 100, 26, 0, 200, 100, 27, 0, 200, 120, 28, 0, 210, 120, 29, 0, 220, 120, 30, 0, 230, 120, 31, 0, 240, 130, 32, 0, 250, 130, 33, 0, 270, 130, 34, 0, 280, 140, 35, 0, 300, 150, 36, 0, 320, 220 };
    private static readonly int[] Corridor50 = { 13, 0, 360, 330, 14, 0, 360, 330, 15, 0, 360, 330, 16, 0, 370, 330, 17, 0, 370, 330, 18, 0, 380, 330, 19, 0, 380, 330, 20, 0, 380, 330, 21, 0, 390, 330, 22, 0, 400, 330, 23, 0, 410, 330, 24, 0, 410, 330, 25, 0, 420, 320, 26, 0, 420, 320, 27, 0, 420, 310, 28, 0, 420, 300, 29, 0, 420, 290, 30, 0, 420, 280, 31, 0, 410, 280, 32, 0, 410, 270, 33, 0, 330, 260, 34, 0, 320, 250, 35, 0, 310, 240, 36, 0, 290, 230, 37, 0, 290, 220, 38, 0, 280, 210, 39, 0, 270, 210, 40, 0, 260, 190, 41, 0, 250, 190, 42, 0, 240, 180, 43, 0, 230, 170, 44, 0, 230, 150, 45, 0, 220, 140, 46, 0, 210, 130, 47, 0, 210, 130, 48, 0, 200, 120, 49, 0, 200, 120, 50, 0, 200, 110, 51, 0, 200, 110, 52, 0, 200, 110, 53, 0, 200, 100, 54, 0, 190, 100, 55, 0, 190, 110, 56, 0, 190, 110, 57, 0, 180, 120, 58, 0, 180, 120, 59, 0, 170, 120, 60, 0, 170, 120, 61, 0, 160, 120, 62, 0, 160, 130, 63, 0, 150, 130, 64, 0, 150, 130 };
    private static readonly int[] Corridor51 = { 43, 0, 466, 380, 44, 0, 478, 360, 45, 0, 490, 350, 46, 0, 500, 340, 47, 0, 500, 330, 48, 0, 360, 330, 48, 405, 505, 330 };
    private static readonly int[] Corridor52 = { 41, 0, 530, 480, 42, 0, 530, 470, 43, 0, 510, 450, 44, 0, 478, 440, 44, 497, 500, 440, 45, 0, 475, 430, 46, 0, 470, 410, 47, 0, 460, 390, 48, 0, 425, 360, 49, 0, 421, 360, 50, 0, 410, 350, 51, 0, 400, 350, 52, 0, 390, 340, 53, 0, 357, 340, 54, 0, 356, 340, 55, 0, 380, 340, 56, 0, 370, 340, 57, 0, 370, 340, 58, 0, 370, 350 };
    private static readonly int[] Corridor53 = { 40, 0, 580, 540, 41, 0, 568, 490, 42, 0, 557, 490, 43, 0, 547, 490, 44, 0, 540, 490, 45, 0, 525, 500, 46, 0, 525, 500, 47, 0, 530, 510, 48, 0, 530, 510, 49, 0, 540, 520, 50, 0, 550, 520, 51, 0, 550, 530, 52, 0, 560, 530, 53, 0, 560, 540, 54, 0, 570, 540, 55, 0, 570, 540 };
    private static readonly int[] Corridor54 = { 2, 0, 118, 64, 3, 0, 126, 64, 4, 0, 131, 64, 5, 0, 134, 64, 6, 0, 135, 64, 7, 0, 137, 64, 8, 0, 138, 64, 9, 0, 138, 64, 10, 0, 137, 64, 11, 0, 135, 64, 12, 0, 130, 64, 13, 53, 125, 64, 14, 62, 117, 64, 15, 70, 94, 64, 13, 0, 53, -1, 14, 0, 62, -1, 15, 0, 70, -1, 16, 0, 75, -1, 17, 0, 77, -1, 18, 0, 78, -1, 19, 0, 78, -1, 20, 0, 73, -1, 21, 0, 68, -1, 22, 0, 63, -1, 23, 0, 58, -1, 24, 0, 54, -1, 25, 0, 52, -1, 26, 0, 51, -1, 27, 0, 51, -1, 28, 0, 51, -1 };
    private static readonly int[] Corridor55 = { 1, 0, 54, 0, 2, 0, 58, 0, 3, 0, 63, 0, 4, 0, 66, 0, 5, 0, 68, 0, 6, 0, 69, 0, 7, 0, 70, 0, 8, 0, 70, 0, 9, 0, 70, 0, 10, 0, 69, 0, 11, 0, 67, 0, 12, 0, 63, 0, 13, 0, 59, 0, 14, 0, 54, 0, 15, 0, 48, 0 };
    private static readonly int[] Corridor56 = { 20, 41, 52, -1, 21, 31, 55, -1, 22, 23, 51, -1, 23, 19, 52, -1, 24, 15, 64, -1, 25, 11, 83, -1, 26, 8, 84, -1, 27, 7, 121, -1, 28, 0, 147, -1, 29, 0, 148, -1, 30, 0, 149, -1, 31, 0, 150, -1, 32, 0, 151, -1, 33, 0, 151, -1, 34, 0, 152, -1, 35, 0, 152, -1, 36, 0, 151, -1, 37, 0, 148, -1, 38, 0, 146, -1, 39, 0, 143, -1, 40, 0, 137, -1, 41, 0, 127, -1, 42, 0, 120, -1, 43, 0, 115, -1, 44, 0, 108, -1, 45, 0, 92, -1, 50, 0, 101, 49, 51, 0, 107, 49, 52, 0, 108, 49, 53, 0, 102, 49 };
    private static readonly int[] Corridor57 = { 1, 0, 304, 145, 2, 0, 313, 145, 3, 0, 317, 145, 4, 0, 319, 145, 5, 0, 320, 145, 6, 0, 243, 145, 6, 243, 320, 145, 7, 0, 248, 145, 7, 248, 318, 145, 8, 0, 252, 145, 8, 252, 316, 145, 9, 0, 253, 145, 9, 253, 312, 145, 10, 0, 254, 145, 10, 254, 303, 145, 11, 0, 255, 145, 12, 0, 260, 145, 13, 0, 196, 145, 13, 196, 262, 145, 14, 0, 201, 145, 14, 201, 261, 145, 15, 0, 203, 145, 15, 203, 260, 145, 16, 0, 204, 145, 16, 204, 232, 145, 16, 232, 254, 145, 17, 0, 202, 145, 17, 202, 237, 145, 18, 0, 199, 145, 18, 199, 239, 145, 19, 0, 192, 145, 19, 192, 240, 145, 20, 0, 238, 145, 21, 0, 188, 145, 21, 188, 235, 145, 22, 0, 194, 145, 22, 194, 230, 145, 23, 0, 198, 145, 24, 0, 200, 145, 25, 0, 200, 145, 26, 0, 199, 145, 27, 0, 196, 145, 28, 0, 192, 145, 29, 0, 180, 145 };
    private static readonly int[] Corridor58 = { 20, 0, 184, 145, 21, 0, 193, 145, 22, 0, 197, 145, 23, 0, 199, 145, 24, 0, 200, 145, 25, 0, 199, 145, 26, 0, 197, 145, 27, 0, 193, 145, 27, 193, 231, 145, 28, 0, 184, 145, 28, 184, 236, 145, 29, 0, 239, 145, 30, 0, 194, 145, 30, 194, 240, 145, 31, 0, 200, 145, 31, 200, 239, 145, 32, 0, 203, 145, 32, 203, 236, 145, 33, 0, 204, 145, 33, 204, 231, 145, 33, 231, 255, 145, 34, 0, 203, 145, 34, 203, 260, 145, 35, 0, 200, 145, 35, 200, 262, 145, 36, 0, 195, 145, 36, 195, 261, 145, 37, 0, 260, 145, 38, 0, 254, 145, 39, 0, 254, 145, 39, 254, 305, 145, 40, 0, 253, 145, 40, 253, 313, 145, 41, 0, 251, 145, 41, 251, 317, 145, 42, 0, 248, 145, 42, 248, 319, 145, 43, 0, 242, 145, 43, 242, 320, 145, 44, 0, 320, 145, 45, 0, 318, 145, 46, 0, 316, 145, 47, 0, 311, 145, 48, 0, 301, 145 };
    private static readonly int[] Corridor59 = { 1, 0, 314, 218, 2, 0, 287, 218, 2, 287, 320, 218, 3, 0, 291, 218, 3, 291, 323, 218, 4, 0, 290, 218, 4, 290, 324, 218, 5, 0, 288, 218, 5, 288, 320, 218, 6, 0, 283, 218, 7, 0, 277, 218, 8, 0, 205, 160, 8, 205, 269, 218, 9, 0, 212, 160, 9, 212, 259, 218, 10, 0, 218, 160, 11, 0, 221, 160, 12, 0, 224, 160, 13, 0, 226, 160, 14, 0, 228, 160, 15, 0, 229, 160, 16, 0, 228, 160, 17, 0, 226, 160, 18, 0, 222, 160, 19, 0, 217, 160 };
    private static readonly int[] Corridor60 = { 30, 0, 216, 160, 31, 0, 221, 160, 32, 0, 225, 160, 33, 0, 227, 160, 34, 0, 229, 160, 35, 0, 229, 160, 36, 0, 227, 160, 37, 0, 225, 160, 38, 0, 222, 160, 39, 0, 219, 160, 40, 0, 213, 160, 40, 213, 255, 218, 41, 0, 206, 160, 41, 206, 268, 218, 42, 0, 276, 218, 43, 0, 282, 218, 44, 0, 287, 218, 44, 287, 317, 218, 45, 0, 290, 218, 45, 290, 323, 218, 46, 0, 291, 218, 46, 291, 324, 218, 47, 0, 288, 218, 47, 288, 320, 218, 48, 0, 315, 218 };
    private static readonly int[] Corridor61 = { 10, 0, 135, 87, 11, 0, 142, 87, 12, 0, 146, 87, 13, 0, 74, -1, 13, 74, 148, 87, 14, 0, 90, -1, 14, 90, 149, 87, 15, 0, 99, -1, 15, 99, 149, 87, 16, 0, 106, -1, 16, 106, 147, 87, 17, 0, 111, -1, 17, 111, 144, 87, 18, 0, 115, -1, 18, 115, 140, 87, 19, 0, 119, -1, 19, 119, 132, 87, 20, 0, 121, -1, 21, 0, 123, -1, 22, 0, 125, -1, 23, 0, 125, -1, 24, 0, 126, -1, 25, 0, 126, -1, 26, 0, 125, -1, 27, 0, 125, -1, 28, 0, 123, -1, 29, 0, 121, -1, 30, 0, 119, -1, 31, 0, 116, -1, 32, 0, 113, -1, 33, 0, 109, -1, 34, 0, 102, -1, 35, 0, 95, -1, 36, 0, 83, -1 };
    private static readonly int[] Corridor62 = { 19, 0, 23, -1, 20, 0, 27, -1, 21, 0, 30, -1, 22, 0, 31, -1, 23, 0, 31, -1, 24, 0, 36, -1, 25, 0, 42, -1, 26, 0, 69, -1, 27, 0, 77, -1, 28, 0, 85, -1, 29, 0, 102, -1, 30, 0, 112, -1, 31, 0, 113, -1, 32, 0, 113, -1, 33, 0, 108, -1, 34, 0, 92, -1, 34, 92, 251, 204, 35, 0, 76, -1, 35, 76, 136, 45, 35, 136, 254, 204, 36, 0, 67, -1, 36, 67, 150, 45, 36, 150, 255, 204, 37, 0, 60, -1, 37, 60, 162, 45, 37, 162, 256, 204, 38, 0, 56, -1, 38, 56, 169, 45, 38, 169, 259, 204, 39, 0, 52, -1, 39, 52, 175, 45, 39, 175, 263, 204, 40, 0, 50, -1, 40, 50, 180, 45, 40, 180, 266, 204, 41, 0, 50, -1, 41, 50, 185, 45, 41, 185, 269, 204, 42, 0, 50, -1, 42, 50, 188, 45, 42, 188, 278, 204, 43, 0, 191, 45, 43, 191, 281, 204, 44, 0, 193, 45, 44, 193, 283, 204, 45, 0, 195, 45, 45, 195, 283, 204, 46, 0, 196, 45, 46, 196, 281, 204, 47, 0, 197, 45, 47, 197, 279, 204 };
    private static readonly int[] Corridor63 = { 0, 0, 153, 0, 0, 153, 296, 231, 1, 0, 153, 0, 1, 153, 298, 231, 2, 0, 153, 0, 2, 153, 300, 231, 3, 0, 153, 0, 3, 153, 303, 231, 4, 0, 153, 0, 4, 153, 304, 231, 5, 0, 152, 0, 5, 152, 304, 231, 6, 0, 151, 0, 6, 151, 304, 231, 7, 0, 151, 0, 7, 151, 303, 231, 8, 0, 147, 0, 8, 147, 301, 231, 9, 0, 143, 0, 9, 143, 298, 231, 10, 0, 139, 0, 10, 139, 295, 231, 11, 0, 130, 0, 11, 130, 289, 231, 12, 0, 122, 0, 12, 122, 282, 231, 13, 0, 102, 0, 14, 0, 118, 76, 15, 0, 126, 76, 16, 0, 131, 76, 17, 0, 133, 76, 18, 0, 135, 76, 19, 0, 136, 76, 19, 136, 202, 116, 20, 0, 136, 76, 20, 136, 214, 116, 21, 0, 134, 76, 21, 134, 221, 116, 22, 0, 132, 76, 22, 132, 227, 116, 23, 0, 129, 76, 23, 129, 231, 116, 24, 0, 123, 76, 24, 123, 235, 116, 25, 0, 238, 116, 26, 0, 240, 116, 27, 0, 242, 116, 28, 0, 244, 116, 29, 0, 244, 116, 30, 0, 245, 116, 31, 0, 245, 116, 32, 0, 244, 116, 33, 0, 244, 116, 34, 0, 242, 116, 35, 0, 240, 116, 36, 0, 238, 116, 37, 0, 235, 116, 38, 0, 232, 116, 39, 0, 228, 116, 40, 0, 221, 116, 41, 0, 215, 116, 42, 0, 203, 116 };
    private static readonly int[] Corridor64 = { 3, 0, 208, 130, 4, 0, 220, 130, 5, 0, 228, 130, 6, 0, 235, 130, 7, 0, 239, 130, 8, 0, 243, 130, 9, 0, 246, 130, 10, 0, 248, 130, 11, 0, 250, 130, 12, 0, 252, 130, 13, 0, 252, 130, 14, 0, 253, 130, 15, 0, 253, 130, 16, 0, 252, 130, 17, 0, 252, 130, 18, 0, 251, 130, 19, 0, 249, 130, 20, 0, 247, 130, 21, 0, 134, 89, 21, 134, 244, 130, 22, 0, 142, 89, 22, 142, 240, 130, 23, 0, 147, 89, 23, 147, 237, 130, 24, 0, 150, 89, 24, 150, 231, 130, 25, 0, 152, 89, 25, 152, 224, 130, 26, 0, 153, 89, 26, 153, 213, 130, 27, 0, 153, 89, 28, 0, 152, 89, 29, 0, 150, 89, 30, 0, 146, 89, 31, 0, 141, 89, 32, 0, 106, 13, 32, 106, 128, 89, 33, 0, 122, 13, 34, 0, 133, 13, 34, 133, 293, 245, 35, 0, 140, 13, 35, 140, 299, 245, 36, 0, 148, 13, 36, 148, 305, 245, 37, 0, 152, 13, 37, 152, 309, 245, 38, 0, 156, 13, 38, 156, 313, 245, 39, 0, 160, 13, 39, 160, 316, 245, 40, 0, 164, 13, 40, 164, 319, 245, 41, 0, 166, 13, 41, 166, 319, 245, 42, 0, 169, 13, 42, 169, 320, 245, 43, 0, 172, 13, 43, 172, 320, 245, 44, 0, 175, 13, 44, 175, 318, 245, 45, 0, 175, 13, 45, 175, 314, 245, 46, 0, 176, 13, 46, 176, 228, 185, 46, 228, 310, 245, 47, 0, 177, 13, 47, 177, 238, 185, 47, 238, 303, 245, 48, 0, 178, 13, 48, 178, 244, 185, 48, 244, 294, 245, 49, 0, 177, 13, 49, 177, 248, 185, 50, 0, 176, 13, 50, 176, 251, 185, 51, 0, 175, 13, 51, 175, 252, 185, 52, 0, 175, 13, 52, 175, 252, 185 };
    private static readonly int[] Corridor65 = { 0, 0, 47, 0, 0, 47, 185, 43, 0, 185, 264, 205, 1, 0, 47, 0, 1, 47, 182, 43, 1, 182, 263, 205, 2, 0, 47, 0, 2, 47, 179, 43, 2, 179, 263, 205, 3, 0, 47, 0, 3, 47, 171, 43, 3, 171, 266, 205, 4, 0, 48, 0, 4, 48, 164, 43, 4, 164, 268, 205, 5, 0, 50, 0, 5, 50, 157, 43, 5, 157, 267, 205, 6, 0, 52, 0, 6, 52, 144, 43, 6, 144, 263, 205, 7, 0, 56, 0, 7, 56, 131, 43, 7, 131, 260, 205, 8, 0, 62, 0, 9, 0, 66, 0, 10, 0, 67, 0, 11, 0, 65, 0, 12, 0, 61, 0, 13, 0, 48, 0, 14, 0, 40, 0, 15, 0, 36, 0, 16, 0, 31, 0 };
    private static readonly int[] Corridor66 = { 31, 0, 246, 177, 32, 0, 257, 177, 33, 0, 284, 177, 34, 0, 295, 177, 35, 0, 306, 177, 36, 0, 311, 177, 37, 0, 315, 177, 38, 0, 318, 177, 39, 0, 320, 177, 40, 0, 323, 177, 41, 0, 324, 177, 42, 0, 326, 177, 43, 0, 327, 177, 44, 0, 328, 177, 45, 0, 329, 177, 46, 0, 330, 177, 47, 0, 330, 177, 48, 0, 330, 177, 49, 0, 330, 177, 50, 0, 329, 177, 51, 0, 328, 177, 52, 0, 321, 177, 53, 0, 304, 177, 53, 318, 320, 177, 54, 0, 299, 177, 55, 0, 297, 177, 56, 0, 288, 177, 57, 0, 277, 177, 58, 0, 206, 130, 59, 0, 226, 130, 60, 0, 229, 130, 61, 0, 225, 130, 62, 0, 221, 130, 63, 0, 216, 130, 64, 0, 211, 130, 65, 0, 207, 130, 66, 0, 202, 130, 67, 0, 197, 130, 68, 0, 192, 130, 69, 0, 188, 130, 70, 0, 183, 130, 71, 0, 175, 130, 72, 0, 166, 130 };
    private static readonly int[] Corridor67 = { 19, 0, 257, 177, 20, 0, 286, 177, 21, 0, 290, 177, 22, 0, 297, 177, 23, 0, 304, 177, 24, 0, 307, 177, 24, 317, 321, 177, 25, 0, 327, 177, 26, 0, 328, 177, 27, 0, 329, 177, 28, 0, 330, 177, 29, 0, 330, 177, 30, 0, 330, 177, 31, 0, 329, 177, 32, 0, 329, 177, 33, 0, 328, 177, 34, 0, 327, 177, 35, 0, 325, 177, 36, 0, 324, 177, 37, 0, 322, 177, 38, 0, 319, 177, 39, 0, 317, 177, 40, 0, 313, 177, 41, 0, 310, 177, 42, 0, 302, 177, 43, 0, 291, 177, 44, 0, 277, 177, 45, 0, 254, 177, 4, 0, 161, 130, 5, 0, 170, 130, 6, 0, 179, 130, 7, 0, 185, 130, 8, 0, 189, 130, 9, 0, 194, 130, 10, 0, 199, 130, 11, 0, 204, 130, 12, 0, 209, 130, 13, 0, 213, 130, 14, 0, 218, 130, 15, 0, 222, 130, 16, 0, 227, 130, 17, 0, 231, 130, 18, 0, 218, 130 };
    private static readonly int[] Corridor68 = { 29, 0, 275, 120, 30, 0, 283, 120, 31, 0, 293, 120, 32, 0, 301, 120, 33, 0, 309, 120, 34, 0, 95, 180, 34, 95, 317, 120, 35, 0, 180, 180, 35, 180, 324, 120, 36, 0, 180, 180, 36, 180, 329, 120, 37, 0, 180, 180, 37, 180, 335, 120, 38, 0, 180, 180, 38, 180, 341, 120, 39, 0, 180, 180, 39, 180, 347, 120, 40, 0, 80, 180, 40, 80, 95, 215, 40, 95, 215, 215, 40, 215, 353, 120, 41, 0, 80, 120, 41, 80, 215, 215, 41, 215, 359, 120, 42, 0, 80, 120, 42, 80, 215, 215, 42, 215, 365, 120, 43, 0, 80, 300, 43, 80, 155, 215, 43, 155, 367, 300, 44, 0, 80, 300, 44, 80, 155, 215, 44, 155, 367, 300, 45, 0, 80, 295, 45, 80, 155, 215, 45, 155, 367, 295, 46, 0, 80, 290, 46, 80, 155, 215, 46, 155, 367, 290, 47, 0, 80, 290, 47, 80, 155, 215, 47, 155, 367, 290, 48, 0, 367, 295, 49, 0, 367, 300, 50, 0, 367, 305, 51, 0, 367, 310, 52, 0, 367, 315, 53, 0, 367, 320, 54, 0, 367, 325, 55, 0, 367, 330, 56, 0, 367, 335, 57, 0, 367, 340, 58, 0, 367, 345, 59, 0, 367, 350 };
    private static readonly int[] Corridor69 = { 31, 0, 246, 177, 32, 0, 257, 177, 33, 0, 284, 177, 34, 0, 295, 177, 35, 0, 306, 177, 36, 0, 311, 177, 37, 0, 315, 177, 38, 0, 318, 177, 39, 0, 320, 177, 40, 0, 323, 177, 41, 0, 324, 177, 42, 0, 326, 177, 43, 0, 327, 177, 44, 0, 328, 177, 45, 0, 329, 177, 46, 0, 330, 177, 47, 0, 330, 177, 48, 0, 330, 177, 49, 0, 330, 177, 50, 0, 329, 177, 51, 0, 328, 177, 52, 0, 321, 177, 53, 0, 304, 177, 53, 318, 320, 177, 54, 0, 299, 177, 55, 0, 297, 177, 56, 0, 288, 177, 57, 0, 277, 177, 58, 0, 206, 130, 59, 0, 226, 130, 60, 0, 229, 130, 61, 0, 225, 130, 62, 0, 221, 130, 63, 0, 216, 130, 64, 0, 211, 130, 65, 0, 207, 130, 66, 0, 202, 130, 67, 0, 197, 130, 68, 0, 192, 130, 69, 0, 188, 130, 70, 0, 183, 130, 71, 0, 175, 130, 72, 0, 166, 130 };
    private static readonly int[] Corridor70 = { 19, 0, 257, 177, 20, 0, 286, 177, 21, 0, 290, 177, 22, 0, 297, 177, 23, 0, 304, 177, 24, 0, 307, 177, 24, 317, 321, 177, 25, 0, 327, 177, 26, 0, 328, 177, 27, 0, 329, 177, 28, 0, 330, 177, 29, 0, 330, 177, 30, 0, 330, 177, 31, 0, 329, 177, 32, 0, 329, 177, 33, 0, 328, 177, 34, 0, 327, 177, 35, 0, 325, 177, 36, 0, 324, 177, 37, 0, 322, 177, 38, 0, 319, 177, 39, 0, 317, 177, 40, 0, 313, 177, 41, 0, 310, 177, 42, 0, 302, 177, 43, 0, 291, 177, 44, 0, 277, 177, 45, 0, 254, 177, 4, 0, 161, 130, 5, 0, 170, 130, 6, 0, 179, 130, 7, 0, 185, 130, 8, 0, 189, 130, 9, 0, 194, 130, 10, 0, 199, 130, 11, 0, 204, 130, 12, 0, 209, 130, 13, 0, 213, 130, 14, 0, 218, 130, 15, 0, 222, 130, 16, 0, 227, 130, 17, 0, 231, 130, 18, 0, 218, 130 };
    private static readonly int[] Corridor71 = { 24, 0, 121, 70, 25, 0, 132, 70, 26, 0, 140, 70, 27, 0, 146, 70, 28, 0, 148, 70, 29, 0, 148, 70, 30, 0, 147, 70, 31, 0, 144, 70, 32, 0, 140, 70, 33, 0, 134, 70, 34, 0, 127, 70 };
    private static readonly int[] Corridor72 = { 23, 223, 295, 188, 24, 209, 312, 188, 25, 199, 323, 188, 26, 193, 337, 188, 27, 190, 352, 188, 28, 192, 396, 188, 29, 199, 402, 188, 30, 206, 398, 188, 31, 214, 394, 188, 32, 225, 389, 188, 33, 235, 383, 188, 34, 243, 376, 188, 35, 250, 369, 188, 36, 257, 362, 188, 37, 262, 356, 188, 38, 267, 350, 188, 39, 271, 343, 188, 40, 293, 332, 188 };
    private static readonly int[] Corridor73 = { 380, 400, 530, 500, 381, 400, 531, 500, 382, 400, 531, 500, 383, 400, 532, 500, 384, 400, 532, 500, 385, 400, 533, 500, 386, 400, 534, 500, 387, 400, 534, 500, 388, 400, 535, 500, 389, 400, 535, 500, 390, 400, 536, 500, 391, 400, 536, 500, 392, 400, 537, 500, 393, 400, 537, 500, 394, 400, 538, 500, 395, 400, 539, 500, 396, 400, 539, 500, 397, 400, 540, 500, 398, 400, 540, 500, 399, 400, 541, 500, 400, 400, 541, 500, 401, 400, 542, 500, 402, 400, 543, 500, 403, 400, 543, 500, 404, 400, 544, 500, 405, 400, 544, 500, 406, 400, 545, 500, 407, 400, 546, 500, 408, 400, 546, 500, 409, 400, 547, 500, 410, 400, 547, 500, 411, 400, 548, 500, 412, 400, 549, 500, 413, 400, 549, 500, 414, 400, 550, 500, 415, 400, 550, 500, 416, 401, 551, 500, 417, 402, 552, 500, 418, 403, 443, 500, 418, 444, 552, 500, 419, 404, 442, 500, 419, 446, 553, 500, 420, 405, 441, 500, 420, 447, 553, 500, 421, 406, 440, 500, 421, 449, 553, 500, 422, 407, 440, 500, 422, 451, 553, 500, 423, 407, 439, 500, 423, 453, 553, 500, 424, 408, 438, 500, 424, 454, 553, 500, 425, 409, 438, 500, 425, 456, 554, 500, 426, 410, 437, 500, 426, 458, 554, 500, 427, 411, 436, 500, 427, 460, 554, 500, 428, 412, 435, 500, 428, 461, 554, 500, 429, 413, 435, 500, 429, 463, 554, 500, 430, 470, 539, 500, 431, 470, 537, 500, 432, 470, 535, 500, 433, 471, 533, 500, 434, 471, 531, 500, 435, 471, 529, 500, 436, 472, 527, 500, 437, 472, 525, 500, 438, 472, 523, 500, 439, 473, 521, 500, 440, 473, 519, 500, 441, 473, 517, 500, 442, 474, 515, 500, 443, 474, 513, 500, 444, 474, 511, 500, 445, 475, 508, 500, 446, 475, 506, 500, 447, 475, 504, 500, 448, 476, 502, 500, 449, 476, 500, 500, 450, 476, 497, 500, 451, 477, 493, 500, 452, 477, 488, 500, 453, 477, 484, 500, 454, 478, 480, 500 };
    private static readonly int[] Corridor74 = { 518, 680, 734, 710, 518, 794, 821, 710, 519, 680, 734, 710, 519, 794, 820, 710, 520, 680, 733, 710, 520, 795, 820, 710, 521, 680, 732, 710, 521, 794, 820, 710, 522, 680, 732, 710, 522, 793, 819, 710, 523, 680, 731, 710, 523, 792, 819, 710, 524, 680, 731, 710, 524, 791, 818, 710, 525, 681, 730, 710, 525, 790, 818, 710, 526, 681, 729, 710, 526, 765, 818, 710, 527, 681, 729, 710, 527, 760, 817, 710, 528, 681, 729, 710, 528, 755, 817, 710, 529, 681, 730, 710, 529, 752, 817, 710, 530, 681, 730, 710, 530, 751, 816, 710, 531, 681, 730, 710, 531, 750, 816, 710, 532, 681, 730, 710, 532, 749, 815, 710, 533, 681, 730, 710, 533, 749, 815, 710, 534, 681, 731, 710, 534, 748, 815, 710, 535, 681, 731, 710, 535, 747, 814, 710, 536, 681, 731, 710, 536, 747, 814, 710, 537, 681, 732, 710, 537, 746, 813, 710, 538, 681, 734, 710, 538, 745, 813, 710, 539, 681, 735, 710, 539, 744, 813, 710, 540, 682, 737, 710, 540, 741, 812, 710, 541, 682, 812, 710, 542, 682, 812, 710, 543, 682, 811, 710, 544, 682, 811, 710, 545, 682, 810, 710, 546, 682, 810, 710, 547, 682, 810, 710, 548, 638, 809, 710, 549, 637, 809, 710, 550, 636, 808, 710, 551, 635, 808, 710, 552, 634, 808, 710, 553, 633, 807, 710, 554, 632, 807, 710, 555, 631, 806, 710, 556, 630, 806, 710, 557, 629, 806, 710, 558, 628, 805, 710, 559, 627, 805, 710, 560, 626, 805, 710, 561, 625, 804, 710, 562, 624, 804, 710, 563, 623, 803, 710, 564, 622, 803, 710, 565, 621, 803, 710, 566, 620, 802, 710, 567, 619, 802, 710, 568, 618, 801, 710, 569, 617, 801, 710, 570, 616, 801, 710, 571, 615, 800, 710, 572, 614, 800, 710, 573, 613, 800, 710, 574, 612, 799, 710, 575, 611, 799, 710, 576, 610, 697, 710, 576, 698, 798, 710, 577, 609, 696, 710, 577, 699, 798, 710, 578, 608, 695, 710, 578, 699, 798, 710, 579, 607, 694, 710, 579, 700, 797, 710 };
    private static readonly int[] Corridor75 = { 99, 0, 58, 0, 100, 0, 60, 0, 101, 0, 62, 0, 102, 0, 64, 0, 103, 0, 65, 0, 104, 0, 65, 0, 105, 0, 65, 0, 106, 0, 65, 0, 107, 0, 66, 0, 108, 0, 66, 0, 109, 0, 66, 0, 110, 0, 66, 0, 111, 0, 67, 0, 112, 0, 68, 0, 113, 0, 69, 0, 114, 0, 70, 0, 115, 0, 71, 0, 116, 0, 72, 0, 117, 0, 72, 0, 118, 0, 73, 0, 119, 0, 74, 0, 120, 0, 74, 0, 121, 0, 75, 0, 122, 0, 75, 0, 123, 0, 76, 0, 124, 0, 77, 0, 125, 0, 77, 0, 126, 0, 78, 0, 127, 0, 78, 0, 128, 0, 79, 0, 129, 0, 80, 0, 130, 0, 80, 0, 131, 0, 81, 0, 132, 0, 81, 0, 133, 0, 82, 0, 134, 0, 83, 0, 135, 0, 83, 0, 136, 0, 84, 0, 137, 0, 84, 0, 138, 0, 84, 0, 139, 0, 85, 0, 140, 0, 85, 0, 141, 0, 85, 0, 142, 0, 85, 0, 143, 0, 86, 0, 144, 0, 86, 0, 145, 0, 86, 0, 146, 0, 87, 0, 147, 0, 87, 0, 148, 0, 88, 0, 149, 0, 89, 0, 150, 0, 89, 0, 151, 0, 90, 0, 152, 0, 91, 0, 153, 0, 91, 0, 154, 0, 92, 0, 155, 0, 92, 0, 156, 0, 93, 0, 157, 0, 93, 0, 158, 0, 94, 0, 159, 0, 94, 0, 160, 0, 94, 0, 161, 0, 95, 0, 162, 0, 95, 0, 163, 0, 96, 0, 164, 0, 96, 0, 165, 0, 97, 0, 166, 0, 97, 0, 167, 0, 97, 0, 168, 0, 97, 0, 169, 0, 97, 0, 170, 0, 97, 0, 171, 0, 98, 0, 172, 0, 98, 0, 173, 0, 98, 0, 174, 0, 98, 0, 175, 0, 98, 0, 176, 0, 99, 0, 177, 0, 99, 0, 178, 0, 99, 0, 179, 0, 99, 0, 180, 0, 99, 0, 181, 0, 99, 0, 182, 0, 100, 0, 183, 0, 99, 0, 184, 0, 99, 0, 185, 0, 99, 0, 186, 0, 98, 0, 187, 0, 98, 0, 188, 0, 98, 0, 189, 0, 97, 0, 190, 0, 97, 0, 191, 0, 97, 0, 192, 0, 96, 0, 193, 0, 96, 0, 194, 0, 96, 0, 195, 0, 96, 0, 196, 0, 96, 0, 197, 0, 96, 0, 198, 0, 96, 0, 199, 0, 96, 0, 200, 0, 96, 0, 201, 0, 96, 0, 202, 0, 96, 0, 203, 0, 96, 0, 204, 0, 96, 0, 205, 0, 96, 0, 206, 0, 96, 0, 207, 0, 96, 0, 208, 0, 96, 0, 209, 0, 96, 0, 210, 0, 96, 0, 211, 0, 96, 0, 212, 0, 96, 0, 213, 0, 96, 0, 214, 0, 96, 0, 215, 0, 96, 0, 216, 0, 97, 0, 217, 0, 97, 0, 218, 0, 97, 0, 219, 0, 97, 0, 220, 0, 98, 0, 221, 0, 98, 0, 222, 0, 98, 0, 223, 0, 98, 0, 224, 0, 99, 0, 225, 0, 99, 0, 226, 0, 99, 0, 227, 0, 100, 0, 228, 0, 100, 0, 229, 0, 100, 0, 230, 0, 100, 0, 231, 0, 101, 0, 232, 0, 101, 0, 233, 0, 101, 0, 234, 0, 102, 0, 235, 0, 102, 0, 236, 0, 102, 0, 237, 0, 102, 0, 238, 0, 102, 0, 239, 0, 102, 0, 240, 0, 102, 0, 241, 0, 102, 0, 242, 0, 102, 0, 243, 0, 102, 0, 244, 0, 103, 0, 245, 0, 103, 0, 246, 0, 103, 0, 247, 0, 103, 0, 248, 0, 103, 0, 249, 0, 103, 0, 250, 0, 103, 0, 251, 0, 103, 0, 252, 0, 103, 0, 253, 0, 104, 0, 254, 0, 104, 0, 255, 0, 104, 0, 256, 0, 104, 0, 257, 0, 104, 0, 258, 0, 104, 0, 259, 0, 104, 0, 260, 0, 104, 0, 261, 0, 104, 0, 262, 0, 105, 0, 263, 0, 105, 0, 264, 0, 105, 0, 265, 0, 105, 0, 266, 0, 105, 0, 267, 0, 105, 0, 268, 0, 105, 0, 269, 0, 105, 0, 270, 0, 105, 0, 271, 0, 106, 0, 272, 0, 106, 0, 273, 0, 106, 0, 274, 0, 106, 0, 275, 0, 106, 0, 276, 0, 106, 0, 277, 0, 106, 0, 278, 0, 106, 0, 279, 0, 106, 0, 280, 0, 107, 0, 281, 0, 107, 0, 282, 0, 107, 0, 283, 0, 107, 0, 284, 0, 108, 0, 285, 0, 108, 0, 286, 0, 108, 0, 287, 0, 108, 0, 288, 0, 109, 0, 289, 0, 109, 0, 290, 0, 109, 0, 291, 0, 109, 0, 292, 0, 109, 0, 293, 0, 110, 0, 294, 0, 110, 0, 295, 0, 110, 0, 296, 0, 110, 0, 297, 0, 111, 0, 298, 0, 111, 0, 299, 0, 111, 0, 300, 0, 111, 0, 301, 0, 112, 0, 302, 0, 112, 0, 303, 0, 112, 0, 304, 0, 112, 0, 305, 0, 112, 0, 306, 0, 112, 0, 307, 0, 112, 0, 308, 0, 112, 0, 309, 0, 112, 0, 310, 0, 112, 0, 311, 0, 112, 0, 312, 0, 112, 0, 313, 0, 112, 0, 314, 0, 112, 0, 315, 0, 112, 0, 316, 0, 112, 0, 317, 0, 112, 0, 318, 0, 112, 0, 319, 0, 112, 0, 320, 0, 112, 0, 321, 0, 112, 0, 322, 0, 111, 0, 323, 0, 111, 0, 324, 0, 110, 0, 325, 0, 109, 0, 326, 0, 109, 0, 327, 0, 108, 0, 328, 0, 107, 0, 329, 0, 107, 0, 330, 0, 106, 0, 331, 0, 106, 0, 332, 0, 105, 0, 333, 0, 104, 0, 334, 0, 104, 0, 335, 0, 103, 0, 336, 0, 102, 0, 337, 0, 102, 0, 338, 0, 101, 0, 339, 0, 99, 0, 340, 0, 98, 0, 341, 0, 96, 0, 342, 0, 95, 0, 343, 0, 93, 0, 344, 0, 92, 0, 345, 0, 91, 0, 346, 0, 89, 0, 347, 0, 88, 0, 348, 0, 86, 0, 349, 0, 85, 0, 350, 0, 84, 0, 351, 0, 82, 0, 352, 0, 81, 0, 353, 0, 80, 0, 354, 0, 78, 0, 355, 0, 77, 0, 356, 0, 76, 0, 357, 0, 74, 0, 358, 0, 73, 0, 359, 0, 72, 0, 360, 0, 72, 0, 361, 0, 71, 0, 362, 0, 70, 0, 363, 0, 70, 0, 364, 0, 69, 0, 365, 0, 68, 0, 366, 0, 68, 0, 367, 0, 67, 0, 368, 0, 67, 0, 369, 0, 66, 0, 370, 0, 65, 0, 371, 0, 65, 0, 372, 0, 64, 0, 373, 0, 63, 0, 374, 0, 62, 0, 375, 0, 61, 0, 376, 0, 59, 0, 377, 0, 58, 0, 378, 0, 57, 0, 379, 0, 56, 0 };
    private static readonly int[] Corridor76 = { 25, 0, 66, 0, 26, 0, 68, 0, 27, 0, 70, 0, 28, 0, 72, 0, 29, 0, 74, 0, 30, 0, 75, 0, 31, 0, 76, 0, 32, 0, 77, 0, 33, 0, 78, 0, 34, 0, 79, 0, 35, 0, 80, 0, 36, 0, 81, 0, 37, 0, 82, 0, 38, 0, 83, 0, 39, 0, 84, 0, 40, 0, 85, 0, 41, 0, 85, 0, 42, 0, 86, 0, 43, 0, 87, 0, 44, 0, 88, 0, 45, 0, 89, 0, 46, 0, 89, 0, 47, 0, 90, 0, 48, 0, 91, 0, 49, 0, 92, 0, 50, 0, 93, 0, 51, 0, 94, 0, 52, 0, 94, 0, 53, 0, 95, 0, 54, 0, 96, 0, 55, 0, 97, 0, 56, 0, 98, 0, 57, 0, 99, 0, 58, 0, 99, 0, 59, 0, 100, 0, 60, 0, 101, 0, 61, 0, 101, 0, 62, 0, 102, 0, 63, 0, 103, 0, 64, 0, 103, 0, 65, 0, 104, 0, 66, 0, 105, 0, 67, 0, 105, 0, 68, 0, 106, 0, 69, 0, 107, 0, 70, 0, 107, 0, 71, 0, 108, 0, 72, 0, 109, 0, 73, 0, 109, 0, 74, 0, 110, 0, 75, 0, 110, 0, 76, 0, 110, 0, 77, 0, 111, 0, 78, 0, 111, 0, 79, 0, 112, 0, 80, 0, 112, 0, 81, 0, 113, 0, 82, 0, 113, 0, 83, 0, 114, 0, 84, 0, 114, 0, 85, 0, 114, 0, 86, 0, 115, 0, 87, 0, 115, 0, 88, 0, 116, 0, 89, 0, 116, 0, 90, 0, 116, 0, 91, 0, 117, 0, 92, 0, 117, 0, 93, 0, 117, 0, 94, 0, 117, 0, 95, 0, 118, 0, 96, 0, 118, 0, 97, 0, 118, 0, 98, 0, 119, 0, 99, 0, 119, 0, 100, 0, 119, 0, 101, 0, 120, 0, 102, 0, 120, 0, 103, 0, 120, 0, 104, 0, 120, 0, 105, 0, 121, 0, 106, 0, 121, 0, 107, 0, 121, 0, 108, 0, 122, 0, 109, 0, 122, 0, 110, 0, 122, 0, 111, 0, 122, 0, 112, 0, 122, 0, 113, 0, 122, 0, 114, 0, 122, 0, 115, 0, 123, 0, 116, 0, 123, 0, 117, 0, 123, 0, 118, 0, 123, 0, 119, 0, 123, 0, 120, 0, 123, 0, 121, 0, 123, 0, 122, 0, 124, 0, 123, 0, 124, 0, 124, 0, 124, 0, 125, 0, 124, 0, 126, 0, 124, 0, 127, 0, 124, 0, 128, 0, 124, 0, 129, 0, 125, 0, 130, 0, 125, 0, 131, 0, 125, 0, 132, 0, 125, 0, 133, 0, 125, 0, 134, 0, 125, 0, 135, 0, 125, 0, 136, 0, 125, 0, 137, 0, 125, 0, 138, 0, 125, 0, 139, 0, 125, 0, 140, 0, 125, 0, 141, 0, 125, 0, 142, 0, 125, 0, 143, 0, 125, 0, 144, 0, 125, 0, 145, 0, 125, 0, 146, 0, 125, 0, 147, 0, 125, 0, 148, 0, 125, 0, 149, 0, 125, 0, 150, 0, 125, 0, 151, 0, 126, 0, 152, 0, 125, 0, 153, 0, 125, 0, 154, 0, 125, 0, 155, 0, 125, 0, 156, 0, 125, 0, 157, 0, 125, 0, 158, 0, 125, 0, 159, 0, 125, 0, 160, 0, 125, 0, 161, 0, 125, 0, 162, 0, 125, 0, 163, 0, 125, 0, 164, 0, 125, 0, 165, 0, 124, 0, 166, 0, 124, 0, 167, 0, 124, 0, 168, 0, 124, 0, 169, 0, 124, 0, 170, 0, 124, 0, 171, 0, 124, 0, 172, 0, 124, 0, 173, 0, 124, 0, 174, 0, 124, 0, 175, 0, 124, 0, 176, 0, 124, 0, 177, 0, 124, 0, 178, 0, 123, 0, 179, 0, 123, 0, 180, 0, 122, 0, 181, 0, 122, 0, 182, 0, 122, 0, 183, 0, 121, 0, 184, 0, 121, 0, 185, 0, 120, 0, 186, 0, 120, 0, 187, 0, 120, 0, 188, 0, 119, 0, 189, 0, 119, 0, 190, 0, 118, 0, 191, 0, 118, 0, 192, 0, 118, 0, 193, 0, 109, 0, 193, 109, 114, 88, 193, 114, 117, 0, 194, 0, 105, 0, 194, 105, 119, 88, 195, 0, 102, 0, 195, 102, 125, 88, 196, 0, 102, 0, 196, 102, 128, 88, 197, 0, 101, 0, 197, 101, 130, 88, 198, 0, 100, 0, 198, 100, 131, 88, 199, 0, 100, 0, 199, 100, 133, 88, 200, 0, 99, 0, 200, 99, 135, 88, 201, 0, 98, 0, 201, 98, 136, 88, 202, 0, 98, 0, 202, 98, 138, 88, 203, 0, 97, 0, 203, 97, 139, 88, 204, 0, 96, 0, 204, 96, 140, 88, 205, 0, 96, 0, 205, 96, 140, 88, 206, 0, 95, 0, 206, 95, 141, 88, 207, 0, 95, 0, 207, 95, 142, 88, 208, 0, 95, 0, 208, 95, 143, 88, 209, 0, 94, 0, 209, 94, 144, 88, 210, 0, 94, 0, 210, 94, 144, 88, 211, 0, 93, 0, 211, 93, 145, 88, 212, 0, 93, 0, 212, 93, 146, 88, 213, 0, 92, 0, 213, 92, 146, 88, 214, 0, 92, 0, 214, 92, 146, 88, 215, 0, 92, 0, 215, 92, 147, 88, 216, 0, 91, 0, 216, 91, 147, 88, 217, 0, 91, 0, 217, 91, 148, 88, 218, 0, 91, 0, 218, 91, 148, 88, 219, 0, 91, 0, 219, 91, 148, 88, 220, 0, 90, 0, 220, 90, 149, 88, 221, 0, 90, 0, 221, 90, 149, 88, 222, 0, 90, 0, 222, 90, 150, 88, 223, 0, 89, 0, 223, 89, 150, 88, 224, 0, 89, 0, 224, 89, 150, 88, 225, 0, 89, 0, 225, 89, 151, 88, 226, 0, 89, 0, 226, 89, 150, 88, 227, 0, 89, 0, 227, 89, 150, 88, 228, 0, 89, 0, 228, 89, 150, 88, 229, 0, 89, 0, 229, 89, 150, 88, 230, 0, 89, 0, 230, 89, 150, 88, 231, 0, 89, 0, 231, 89, 150, 88, 232, 0, 89, 0, 232, 89, 150, 88, 233, 0, 89, 0, 233, 89, 150, 88, 234, 0, 89, 0, 234, 89, 150, 88, 235, 0, 88, 0, 235, 88, 150, 88, 236, 0, 89, 0, 236, 89, 150, 88, 237, 0, 89, 0, 237, 89, 150, 88, 238, 0, 89, 0, 238, 89, 150, 88, 239, 0, 89, 0, 239, 89, 150, 88, 240, 0, 89, 0, 240, 89, 150, 88, 241, 0, 89, 0, 241, 89, 150, 88, 242, 0, 89, 0, 242, 89, 149, 88, 243, 0, 89, 0, 243, 89, 149, 88, 244, 0, 89, 0, 244, 89, 148, 88, 245, 0, 89, 0, 245, 89, 148, 88, 246, 0, 90, 0, 246, 90, 147, 88, 247, 0, 90, 0, 247, 90, 147, 88, 248, 0, 90, 0, 248, 90, 146, 88, 249, 0, 90, 0, 249, 90, 146, 88, 250, 0, 90, 0, 250, 90, 146, 88, 251, 0, 90, 0, 251, 90, 145, 88, 252, 0, 90, 0, 252, 90, 145, 88, 253, 0, 90, 0, 253, 90, 144, 88, 254, 0, 90, 0, 254, 90, 144, 88, 255, 0, 90, 0, 255, 90, 144, 88, 256, 0, 90, 0, 256, 90, 144, 88, 257, 0, 90, 0, 257, 90, 144, 88, 258, 0, 90, 0, 258, 90, 144, 88, 259, 0, 90, 0, 259, 90, 144, 88, 260, 0, 90, 0, 260, 90, 144, 88, 261, 0, 90, 0, 261, 90, 144, 88, 262, 0, 90, 0, 262, 90, 93, 88, 262, 93, 96, 103, 262, 96, 144, 88, 263, 0, 89, 0, 263, 89, 98, 103, 263, 98, 145, 88, 264, 0, 86, 0, 264, 86, 100, 103, 264, 100, 145, 88, 265, 0, 85, 0, 265, 85, 102, 103, 265, 102, 146, 88, 266, 0, 85, 0, 266, 85, 102, 103, 266, 102, 146, 88, 267, 0, 84, 0, 267, 84, 102, 103, 267, 102, 147, 88, 268, 0, 83, 0, 268, 83, 103, 103, 268, 103, 147, 88, 269, 0, 83, 0, 269, 83, 103, 103, 269, 103, 147, 88, 270, 0, 82, 0, 270, 82, 103, 103, 270, 103, 148, 88, 271, 0, 81, 0, 271, 81, 104, 103, 271, 104, 148, 88, 272, 0, 80, 0, 272, 80, 104, 103, 272, 104, 149, 88, 273, 0, 80, 0, 273, 80, 105, 103, 273, 105, 149, 88, 274, 0, 79, 0, 274, 79, 105, 103, 274, 105, 150, 88, 275, 0, 78, 0, 275, 78, 105, 103, 275, 105, 149, 88, 276, 0, 77, 0, 276, 77, 106, 103, 276, 106, 149, 88, 277, 0, 77, 0, 277, 78, 105, 103, 277, 105, 149, 88, 278, 76, 105, 103, 278, 105, 149, 88, 279, 77, 105, 103, 279, 105, 148, 88, 280, 77, 105, 103, 280, 105, 148, 88, 281, 78, 105, 103, 281, 105, 148, 88, 282, 78, 105, 103, 282, 105, 147, 88, 283, 79, 105, 103, 283, 105, 147, 88, 284, 81, 105, 103, 284, 105, 147, 88, 285, 85, 105, 103, 285, 105, 147, 88, 286, 86, 105, 103, 286, 105, 146, 88, 287, 86, 105, 103, 287, 105, 146, 88, 288, 86, 105, 103, 288, 105, 146, 88, 289, 87, 105, 103, 289, 105, 145, 88, 290, 87, 105, 103, 290, 105, 145, 88, 291, 86, 105, 103, 291, 105, 144, 88, 292, 87, 105, 103, 292, 105, 144, 88, 293, 87, 105, 103, 293, 105, 144, 88, 294, 88, 105, 103, 294, 105, 143, 88, 295, 89, 105, 103, 295, 105, 143, 88, 296, 90, 105, 103, 296, 105, 143, 88, 297, 92, 104, 103, 297, 104, 142, 88, 298, 94, 103, 103, 298, 103, 142, 88, 299, 96, 101, 103, 299, 102, 141, 88, 300, 98, 100, 103, 300, 102, 141, 88, 301, 103, 140, 88, 302, 103, 140, 88, 303, 103, 139, 88, 304, 104, 139, 88, 305, 105, 138, 88, 306, 106, 138, 88, 307, 107, 137, 88, 308, 107, 136, 88, 309, 108, 135, 88, 310, 109, 133, 88, 311, 111, 132, 88, 312, 113, 131, 88, 313, 115, 130, 88, 314, 117, 123, 88 };
}
}
