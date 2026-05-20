// Auto-generated ITU-T T.4 Modified Huffman tables

namespace HotlineValetudo.Fax;

public static partial class T4ModifiedHuffman
{
    private static readonly (int value, int length)[] WhiteTerm = new (int, int)[64];

    private static readonly (int value, int length)[] BlackTerm = new (int, int)[64];

    private static readonly (int value, int length)[] WhiteMakeup = new (int, int)[40];

    private static readonly (int value, int length)[] BlackMakeup = new (int, int)[40];


    static T4ModifiedHuffman()
    {
        InitWhiteTerm();
        InitBlackTerm();
        InitWhiteMakeup();
        InitBlackMakeup();
    }

    private static void InitWhiteTerm()
    {
        WhiteTerm[0] = (53, 8); // 0
        WhiteTerm[1] = (7, 6); // 1
        WhiteTerm[2] = (7, 4); // 2
        WhiteTerm[3] = (8, 4); // 3
        WhiteTerm[4] = (11, 4); // 4
        WhiteTerm[5] = (12, 4); // 5
        WhiteTerm[6] = (14, 4); // 6
        WhiteTerm[7] = (15, 4); // 7
        WhiteTerm[8] = (19, 5); // 8
        WhiteTerm[9] = (20, 5); // 9
        WhiteTerm[10] = (7, 5); // 10
        WhiteTerm[11] = (8, 5); // 11
        WhiteTerm[12] = (8, 6); // 12
        WhiteTerm[13] = (3, 6); // 13
        WhiteTerm[14] = (52, 6); // 14
        WhiteTerm[15] = (53, 6); // 15
        WhiteTerm[16] = (42, 6); // 16
        WhiteTerm[17] = (43, 6); // 17
        WhiteTerm[18] = (39, 7); // 18
        WhiteTerm[19] = (12, 7); // 19
        WhiteTerm[20] = (8, 7); // 20
        WhiteTerm[21] = (23, 7); // 21
        WhiteTerm[22] = (3, 7); // 22
        WhiteTerm[23] = (4, 7); // 23
        WhiteTerm[24] = (40, 7); // 24
        WhiteTerm[25] = (43, 7); // 25
        WhiteTerm[26] = (19, 7); // 26
        WhiteTerm[27] = (36, 7); // 27
        WhiteTerm[28] = (24, 7); // 28
        WhiteTerm[29] = (2, 8); // 29
        WhiteTerm[30] = (3, 8); // 30
        WhiteTerm[31] = (26, 8); // 31
        WhiteTerm[32] = (27, 8); // 32
        WhiteTerm[33] = (18, 8); // 33
        WhiteTerm[34] = (19, 8); // 34
        WhiteTerm[35] = (20, 8); // 35
        WhiteTerm[36] = (21, 8); // 36
        WhiteTerm[37] = (22, 8); // 37
        WhiteTerm[38] = (23, 8); // 38
        WhiteTerm[39] = (40, 8); // 39
        WhiteTerm[40] = (41, 8); // 40
        WhiteTerm[41] = (42, 8); // 41
        WhiteTerm[42] = (43, 8); // 42
        WhiteTerm[43] = (44, 8); // 43
        WhiteTerm[44] = (45, 8); // 44
        WhiteTerm[45] = (4, 8); // 45
        WhiteTerm[46] = (5, 8); // 46
        WhiteTerm[47] = (10, 8); // 47
        WhiteTerm[48] = (11, 8); // 48
        WhiteTerm[49] = (82, 8); // 49
        WhiteTerm[50] = (83, 8); // 50
        WhiteTerm[51] = (84, 8); // 51
        WhiteTerm[52] = (85, 8); // 52
        WhiteTerm[53] = (36, 8); // 53
        WhiteTerm[54] = (37, 8); // 54
        WhiteTerm[55] = (88, 8); // 55
        WhiteTerm[56] = (89, 8); // 56
        WhiteTerm[57] = (90, 8); // 57
        WhiteTerm[58] = (91, 8); // 58
        WhiteTerm[59] = (74, 8); // 59
        WhiteTerm[60] = (75, 8); // 60
        WhiteTerm[61] = (50, 8); // 61
        WhiteTerm[62] = (51, 8); // 62
        WhiteTerm[63] = (52, 8); // 63
    }

    private static void InitBlackTerm()
    {
        BlackTerm[0] = (55, 10); // 0
        BlackTerm[1] = (2, 3); // 1
        BlackTerm[2] = (3, 2); // 2
        BlackTerm[3] = (2, 2); // 3
        BlackTerm[4] = (3, 3); // 4
        BlackTerm[5] = (3, 4); // 5
        BlackTerm[6] = (2, 4); // 6
        BlackTerm[7] = (3, 5); // 7
        BlackTerm[8] = (5, 6); // 8
        BlackTerm[9] = (4, 6); // 9
        BlackTerm[10] = (4, 7); // 10
        BlackTerm[11] = (5, 7); // 11
        BlackTerm[12] = (7, 7); // 12
        BlackTerm[13] = (4, 8); // 13
        BlackTerm[14] = (7, 8); // 14
        BlackTerm[15] = (24, 9); // 15
        BlackTerm[16] = (23, 10); // 16
        BlackTerm[17] = (24, 10); // 17
        BlackTerm[18] = (8, 10); // 18
        BlackTerm[19] = (103, 11); // 19
        BlackTerm[20] = (104, 11); // 20
        BlackTerm[21] = (108, 11); // 21
        BlackTerm[22] = (55, 11); // 22
        BlackTerm[23] = (40, 11); // 23
        BlackTerm[24] = (23, 11); // 24
        BlackTerm[25] = (24, 11); // 25
        BlackTerm[26] = (202, 12); // 26
        BlackTerm[27] = (203, 12); // 27
        BlackTerm[28] = (204, 12); // 28
        BlackTerm[29] = (205, 12); // 29
        BlackTerm[30] = (104, 12); // 30
        BlackTerm[31] = (105, 12); // 31
        BlackTerm[32] = (106, 12); // 32
        BlackTerm[33] = (107, 12); // 33
        BlackTerm[34] = (210, 12); // 34
        BlackTerm[35] = (211, 12); // 35
        BlackTerm[36] = (212, 12); // 36
        BlackTerm[37] = (213, 12); // 37
        BlackTerm[38] = (214, 12); // 38
        BlackTerm[39] = (215, 12); // 39
        BlackTerm[40] = (108, 12); // 40
        BlackTerm[41] = (109, 12); // 41
        BlackTerm[42] = (218, 12); // 42
        BlackTerm[43] = (219, 12); // 43
        BlackTerm[44] = (84, 12); // 44
        BlackTerm[45] = (85, 12); // 45
        BlackTerm[46] = (86, 12); // 46
        BlackTerm[47] = (87, 12); // 47
        BlackTerm[48] = (100, 12); // 48
        BlackTerm[49] = (101, 12); // 49
        BlackTerm[50] = (82, 12); // 50
        BlackTerm[51] = (83, 12); // 51
        BlackTerm[52] = (36, 12); // 52
        BlackTerm[53] = (55, 12); // 53
        BlackTerm[54] = (56, 12); // 54
        BlackTerm[55] = (39, 12); // 55
        BlackTerm[56] = (40, 12); // 56
        BlackTerm[57] = (88, 12); // 57
        BlackTerm[58] = (89, 12); // 58
        BlackTerm[59] = (43, 12); // 59
        BlackTerm[60] = (44, 12); // 60
        BlackTerm[61] = (90, 12); // 61
        BlackTerm[62] = (102, 12); // 62
        BlackTerm[63] = (103, 12); // 63
    }

    private static void InitWhiteMakeup()
    {
        WhiteMakeup[0] = (27, 5); // 64
        WhiteMakeup[1] = (18, 5); // 128
        WhiteMakeup[2] = (23, 6); // 192
        WhiteMakeup[3] = (55, 7); // 256
        WhiteMakeup[4] = (54, 8); // 320
        WhiteMakeup[5] = (55, 8); // 384
        WhiteMakeup[6] = (100, 8); // 448
        WhiteMakeup[7] = (101, 8); // 512
        WhiteMakeup[8] = (104, 8); // 576
        WhiteMakeup[9] = (103, 8); // 640
        WhiteMakeup[10] = (204, 9); // 704
        WhiteMakeup[11] = (205, 9); // 768
        WhiteMakeup[12] = (210, 9); // 832
        WhiteMakeup[13] = (211, 9); // 896
        WhiteMakeup[14] = (212, 9); // 960
        WhiteMakeup[15] = (213, 9); // 1024
        WhiteMakeup[16] = (214, 9); // 1088
        WhiteMakeup[17] = (215, 9); // 1152
        WhiteMakeup[18] = (216, 9); // 1216
        WhiteMakeup[19] = (217, 9); // 1280
        WhiteMakeup[20] = (218, 9); // 1344
        WhiteMakeup[21] = (219, 9); // 1408
        WhiteMakeup[22] = (152, 9); // 1472
        WhiteMakeup[23] = (153, 9); // 1536
        WhiteMakeup[24] = (154, 9); // 1600
        WhiteMakeup[25] = (24, 6); // 1664
        WhiteMakeup[26] = (155, 9); // 1728
        WhiteMakeup[27] = (8, 11); // 1792
        WhiteMakeup[28] = (12, 11); // 1856
        WhiteMakeup[29] = (13, 11); // 1920
        WhiteMakeup[30] = (18, 12); // 1984
        WhiteMakeup[31] = (19, 12); // 2048
        WhiteMakeup[32] = (20, 12); // 2112
        WhiteMakeup[33] = (21, 12); // 2176
        WhiteMakeup[34] = (22, 12); // 2240
        WhiteMakeup[35] = (23, 12); // 2304
        WhiteMakeup[36] = (28, 12); // 2368
        WhiteMakeup[37] = (29, 12); // 2432
        WhiteMakeup[38] = (30, 12); // 2496
        WhiteMakeup[39] = (31, 12); // 2560
    }

    private static void InitBlackMakeup()
    {
        BlackMakeup[0] = (15, 10); // 64
        BlackMakeup[1] = (200, 12); // 128
        BlackMakeup[2] = (201, 12); // 192
        BlackMakeup[3] = (91, 12); // 256
        BlackMakeup[4] = (51, 12); // 320
        BlackMakeup[5] = (52, 12); // 384
        BlackMakeup[6] = (53, 12); // 448
        BlackMakeup[7] = (108, 13); // 512
        BlackMakeup[8] = (109, 13); // 576
        BlackMakeup[9] = (74, 13); // 640
        BlackMakeup[10] = (75, 13); // 704
        BlackMakeup[11] = (76, 13); // 768
        BlackMakeup[12] = (77, 13); // 832
        BlackMakeup[13] = (114, 13); // 896
        BlackMakeup[14] = (115, 13); // 960
        BlackMakeup[15] = (116, 13); // 1024
        BlackMakeup[16] = (117, 13); // 1088
        BlackMakeup[17] = (118, 13); // 1152
        BlackMakeup[18] = (119, 13); // 1216
        BlackMakeup[19] = (82, 13); // 1280
        BlackMakeup[20] = (83, 13); // 1344
        BlackMakeup[21] = (84, 13); // 1408
        BlackMakeup[22] = (85, 13); // 1472
        BlackMakeup[23] = (90, 13); // 1536
        BlackMakeup[24] = (91, 13); // 1600
        BlackMakeup[25] = (100, 13); // 1664
        BlackMakeup[26] = (101, 13); // 1728
        BlackMakeup[27] = (8, 11); // 1792
        BlackMakeup[28] = (12, 11); // 1856
        BlackMakeup[29] = (13, 11); // 1920
        BlackMakeup[30] = (18, 12); // 1984
        BlackMakeup[31] = (19, 12); // 2048
        BlackMakeup[32] = (20, 12); // 2112
        BlackMakeup[33] = (21, 12); // 2176
        BlackMakeup[34] = (22, 12); // 2240
        BlackMakeup[35] = (23, 12); // 2304
        BlackMakeup[36] = (28, 12); // 2368
        BlackMakeup[37] = (29, 12); // 2432
        BlackMakeup[38] = (30, 12); // 2496
        BlackMakeup[39] = (31, 12); // 2560
    }
}