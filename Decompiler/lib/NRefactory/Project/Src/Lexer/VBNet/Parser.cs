using System;
using System.Collections;
using System.Collections.Generic;



namespace ICSharpCode.NRefactory.Parser.VB {



partial class ExpressionFinder {
	const int startOfExpression = 56;
	const int endOfStatementTerminatorAndBlock = 267;
	static BitArray GetExpectedSet(int state)
	{
		switch (state) {
			case 0:
			case 1:
				return set[0];
			case 2:
				return set[1];
			case 3:
			case 4:
				return set[2];
			case 5:
				return set[3];
			case 6:
			case 89:
			case 268:
			case 525:
				{
					BitArray a = new BitArray(239);
					return a;
				}
			case 7:
				return set[4];
			case 8:
				return set[5];
			case 9:
			case 10:
			case 20:
				{
					BitArray a = new BitArray(239);
					a.Set(142, true);
					return a;
				}
			case 11:
			case 194:
			case 200:
			case 206:
			case 245:
			case 249:
			case 299:
			case 400:
			case 410:
			case 469:
			case 512:
			case 522:
			case 533:
			case 563:
			case 599:
			case 656:
			case 673:
			case 749:
				return set[6];
			case 12:
			case 13:
			case 564:
			case 565:
			case 610:
			case 620:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(37, true);
					return a;
				}
			case 14:
			case 21:
			case 23:
			case 24:
			case 36:
			case 260:
			case 263:
			case 264:
			case 300:
			case 304:
			case 326:
			case 341:
			case 352:
			case 355:
			case 361:
			case 366:
			case 376:
			case 377:
			case 397:
			case 421:
			case 518:
			case 530:
			case 536:
			case 540:
			case 548:
			case 556:
			case 566:
			case 575:
			case 592:
			case 597:
			case 605:
			case 611:
			case 614:
			case 621:
			case 624:
			case 651:
			case 654:
			case 681:
			case 692:
			case 728:
			case 748:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					return a;
				}
			case 15:
			case 16:
				return set[7];
			case 17:
			case 18:
				return set[8];
			case 19:
			case 261:
			case 275:
			case 302:
			case 356:
			case 398:
			case 449:
			case 573:
			case 593:
			case 612:
			case 616:
			case 622:
			case 652:
			case 693:
				{
					BitArray a = new BitArray(239);
					a.Set(113, true);
					return a;
				}
			case 22:
			case 541:
			case 576:
				return set[9];
			case 25:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					return a;
				}
			case 26:
			case 27:
				return set[10];
			case 28:
			case 732:
				return set[11];
			case 29:
				return set[12];
			case 30:
				return set[13];
			case 31:
			case 32:
			case 153:
			case 218:
			case 219:
			case 269:
			case 280:
			case 281:
			case 436:
			case 437:
			case 457:
			case 458:
			case 459:
			case 460:
			case 551:
			case 552:
			case 585:
			case 586:
			case 687:
			case 688:
			case 741:
			case 742:
				return set[14];
			case 33:
			case 34:
			case 513:
			case 514:
			case 523:
			case 524:
			case 553:
			case 554:
			case 678:
				return set[15];
			case 35:
			case 37:
			case 158:
			case 169:
			case 172:
			case 188:
			case 204:
			case 222:
			case 311:
			case 336:
			case 420:
			case 433:
			case 472:
			case 529:
			case 547:
			case 555:
			case 633:
			case 636:
			case 660:
			case 663:
			case 668:
			case 680:
			case 696:
			case 698:
			case 721:
			case 724:
			case 727:
			case 733:
			case 736:
			case 754:
				return set[16];
			case 38:
			case 41:
				return set[17];
			case 39:
				return set[18];
			case 40:
			case 98:
			case 102:
			case 164:
			case 392:
			case 476:
				return set[19];
			case 42:
			case 178:
			case 185:
			case 190:
			case 254:
			case 443:
			case 468:
			case 471:
			case 587:
			case 588:
			case 648:
				{
					BitArray a = new BitArray(239);
					a.Set(37, true);
					return a;
				}
			case 43:
			case 44:
			case 166:
			case 167:
				return set[20];
			case 45:
			case 168:
			case 189:
			case 257:
			case 446:
			case 470:
			case 473:
			case 490:
			case 521:
			case 528:
			case 559:
			case 590:
			case 627:
			case 630:
			case 642:
			case 650:
			case 667:
			case 684:
			case 702:
			case 731:
			case 740:
				{
					BitArray a = new BitArray(239);
					a.Set(38, true);
					return a;
				}
			case 46:
			case 47:
			case 51:
			case 52:
			case 53:
			case 55:
			case 414:
			case 415:
			case 417:
			case 418:
			case 484:
			case 485:
			case 715:
			case 716:
				return set[21];
			case 48:
			case 49:
				return set[22];
			case 50:
			case 180:
			case 187:
			case 395:
				{
					BitArray a = new BitArray(239);
					a.Set(22, true);
					return a;
				}
			case 54:
			case 170:
			case 171:
			case 173:
			case 182:
			case 416:
			case 419:
			case 423:
			case 431:
			case 480:
			case 483:
			case 487:
			case 497:
			case 504:
			case 511:
			case 717:
				{
					BitArray a = new BitArray(239);
					a.Set(22, true);
					a.Set(38, true);
					return a;
				}
			case 56:
			case 57:
			case 71:
			case 76:
			case 77:
			case 78:
			case 84:
			case 100:
			case 156:
			case 179:
			case 181:
			case 183:
			case 186:
			case 196:
			case 198:
			case 216:
			case 240:
			case 278:
			case 288:
			case 290:
			case 291:
			case 308:
			case 325:
			case 330:
			case 339:
			case 345:
			case 347:
			case 351:
			case 354:
			case 360:
			case 371:
			case 373:
			case 374:
			case 380:
			case 394:
			case 396:
			case 432:
			case 462:
			case 478:
			case 479:
			case 481:
			case 482:
			case 546:
			case 632:
				return set[23];
			case 58:
			case 79:
			case 159:
				return set[24];
			case 59:
				return set[25];
			case 60:
				{
					BitArray a = new BitArray(239);
					a.Set(216, true);
					return a;
				}
			case 61:
				{
					BitArray a = new BitArray(239);
					a.Set(145, true);
					return a;
				}
			case 62:
			case 157:
				{
					BitArray a = new BitArray(239);
					a.Set(144, true);
					return a;
				}
			case 63:
				{
					BitArray a = new BitArray(239);
					a.Set(236, true);
					return a;
				}
			case 64:
				{
					BitArray a = new BitArray(239);
					a.Set(177, true);
					return a;
				}
			case 65:
				{
					BitArray a = new BitArray(239);
					a.Set(175, true);
					return a;
				}
			case 66:
				{
					BitArray a = new BitArray(239);
					a.Set(61, true);
					return a;
				}
			case 67:
				{
					BitArray a = new BitArray(239);
					a.Set(60, true);
					return a;
				}
			case 68:
				{
					BitArray a = new BitArray(239);
					a.Set(150, true);
					return a;
				}
			case 69:
				{
					BitArray a = new BitArray(239);
					a.Set(42, true);
					return a;
				}
			case 70:
				{
					BitArray a = new BitArray(239);
					a.Set(43, true);
					return a;
				}
			case 72:
			case 435:
				{
					BitArray a = new BitArray(239);
					a.Set(40, true);
					return a;
				}
			case 73:
				{
					BitArray a = new BitArray(239);
					a.Set(41, true);
					return a;
				}
			case 74:
			case 99:
			case 223:
			case 224:
			case 286:
			case 287:
			case 338:
			case 406:
			case 750:
				{
					BitArray a = new BitArray(239);
					a.Set(20, true);
					return a;
				}
			case 75:
				{
					BitArray a = new BitArray(239);
					a.Set(154, true);
					return a;
				}
			case 80:
			case 92:
			case 94:
			case 149:
				{
					BitArray a = new BitArray(239);
					a.Set(35, true);
					return a;
				}
			case 81:
			case 82:
				return set[26];
			case 83:
				{
					BitArray a = new BitArray(239);
					a.Set(36, true);
					return a;
				}
			case 85:
			case 101:
			case 507:
				{
					BitArray a = new BitArray(239);
					a.Set(22, true);
					a.Set(36, true);
					return a;
				}
			case 86:
			case 122:
				{
					BitArray a = new BitArray(239);
					a.Set(162, true);
					return a;
				}
			case 87:
			case 88:
				return set[27];
			case 90:
			case 93:
			case 150:
			case 151:
			case 154:
				return set[28];
			case 91:
			case 103:
			case 148:
				{
					BitArray a = new BitArray(239);
					a.Set(233, true);
					return a;
				}
			case 95:
				{
					BitArray a = new BitArray(239);
					a.Set(26, true);
					a.Set(36, true);
					a.Set(147, true);
					return a;
				}
			case 96:
				{
					BitArray a = new BitArray(239);
					a.Set(26, true);
					a.Set(147, true);
					return a;
				}
			case 97:
			case 697:
				{
					BitArray a = new BitArray(239);
					a.Set(26, true);
					return a;
				}
			case 104:
			case 357:
				{
					BitArray a = new BitArray(239);
					a.Set(231, true);
					return a;
				}
			case 105:
				{
					BitArray a = new BitArray(239);
					a.Set(230, true);
					return a;
				}
			case 106:
				{
					BitArray a = new BitArray(239);
					a.Set(224, true);
					return a;
				}
			case 107:
				{
					BitArray a = new BitArray(239);
					a.Set(223, true);
					return a;
				}
			case 108:
			case 303:
				{
					BitArray a = new BitArray(239);
					a.Set(218, true);
					return a;
				}
			case 109:
				{
					BitArray a = new BitArray(239);
					a.Set(213, true);
					return a;
				}
			case 110:
				{
					BitArray a = new BitArray(239);
					a.Set(212, true);
					return a;
				}
			case 111:
				{
					BitArray a = new BitArray(239);
					a.Set(211, true);
					return a;
				}
			case 112:
			case 450:
				{
					BitArray a = new BitArray(239);
					a.Set(210, true);
					return a;
				}
			case 113:
				{
					BitArray a = new BitArray(239);
					a.Set(209, true);
					return a;
				}
			case 114:
				{
					BitArray a = new BitArray(239);
					a.Set(206, true);
					return a;
				}
			case 115:
				{
					BitArray a = new BitArray(239);
					a.Set(203, true);
					return a;
				}
			case 116:
			case 363:
				{
					BitArray a = new BitArray(239);
					a.Set(197, true);
					return a;
				}
			case 117:
			case 598:
			case 617:
				{
					BitArray a = new BitArray(239);
					a.Set(186, true);
					return a;
				}
			case 118:
				{
					BitArray a = new BitArray(239);
					a.Set(184, true);
					return a;
				}
			case 119:
				{
					BitArray a = new BitArray(239);
					a.Set(176, true);
					return a;
				}
			case 120:
				{
					BitArray a = new BitArray(239);
					a.Set(170, true);
					return a;
				}
			case 121:
			case 320:
			case 327:
			case 342:
				{
					BitArray a = new BitArray(239);
					a.Set(163, true);
					return a;
				}
			case 123:
				{
					BitArray a = new BitArray(239);
					a.Set(147, true);
					return a;
				}
			case 124:
			case 227:
			case 232:
			case 234:
				{
					BitArray a = new BitArray(239);
					a.Set(146, true);
					return a;
				}
			case 125:
			case 229:
			case 233:
				{
					BitArray a = new BitArray(239);
					a.Set(143, true);
					return a;
				}
			case 126:
				{
					BitArray a = new BitArray(239);
					a.Set(139, true);
					return a;
				}
			case 127:
				{
					BitArray a = new BitArray(239);
					a.Set(133, true);
					return a;
				}
			case 128:
			case 262:
				{
					BitArray a = new BitArray(239);
					a.Set(127, true);
					return a;
				}
			case 129:
			case 152:
			case 252:
				{
					BitArray a = new BitArray(239);
					a.Set(126, true);
					return a;
				}
			case 130:
				{
					BitArray a = new BitArray(239);
					a.Set(124, true);
					return a;
				}
			case 131:
				{
					BitArray a = new BitArray(239);
					a.Set(121, true);
					return a;
				}
			case 132:
			case 197:
				{
					BitArray a = new BitArray(239);
					a.Set(116, true);
					return a;
				}
			case 133:
				{
					BitArray a = new BitArray(239);
					a.Set(108, true);
					return a;
				}
			case 134:
				{
					BitArray a = new BitArray(239);
					a.Set(107, true);
					return a;
				}
			case 135:
				{
					BitArray a = new BitArray(239);
					a.Set(104, true);
					return a;
				}
			case 136:
			case 643:
				{
					BitArray a = new BitArray(239);
					a.Set(98, true);
					return a;
				}
			case 137:
				{
					BitArray a = new BitArray(239);
					a.Set(87, true);
					return a;
				}
			case 138:
				{
					BitArray a = new BitArray(239);
					a.Set(84, true);
					return a;
				}
			case 139:
			case 209:
			case 239:
				{
					BitArray a = new BitArray(239);
					a.Set(70, true);
					return a;
				}
			case 140:
				{
					BitArray a = new BitArray(239);
					a.Set(67, true);
					return a;
				}
			case 141:
				{
					BitArray a = new BitArray(239);
					a.Set(66, true);
					return a;
				}
			case 142:
				{
					BitArray a = new BitArray(239);
					a.Set(65, true);
					return a;
				}
			case 143:
				{
					BitArray a = new BitArray(239);
					a.Set(64, true);
					return a;
				}
			case 144:
				{
					BitArray a = new BitArray(239);
					a.Set(62, true);
					return a;
				}
			case 145:
			case 251:
				{
					BitArray a = new BitArray(239);
					a.Set(58, true);
					return a;
				}
			case 146:
				{
					BitArray a = new BitArray(239);
					a.Set(2, true);
					return a;
				}
			case 147:
				return set[29];
			case 155:
				return set[30];
			case 160:
				return set[31];
			case 161:
				return set[32];
			case 162:
			case 163:
			case 474:
			case 475:
				return set[33];
			case 165:
				return set[34];
			case 174:
			case 175:
			case 323:
			case 332:
				return set[35];
			case 176:
			case 452:
				return set[36];
			case 177:
			case 379:
				{
					BitArray a = new BitArray(239);
					a.Set(135, true);
					return a;
				}
			case 184:
				return set[37];
			case 191:
				{
					BitArray a = new BitArray(239);
					a.Set(58, true);
					a.Set(126, true);
					return a;
				}
			case 192:
			case 193:
				return set[38];
			case 195:
				{
					BitArray a = new BitArray(239);
					a.Set(171, true);
					return a;
				}
			case 199:
			case 213:
			case 231:
			case 236:
			case 242:
			case 244:
			case 248:
			case 250:
				return set[39];
			case 201:
			case 202:
				{
					BitArray a = new BitArray(239);
					a.Set(63, true);
					a.Set(138, true);
					return a;
				}
			case 203:
			case 205:
			case 324:
				{
					BitArray a = new BitArray(239);
					a.Set(138, true);
					return a;
				}
			case 207:
			case 208:
			case 210:
			case 212:
			case 214:
			case 215:
			case 225:
			case 230:
			case 235:
			case 243:
			case 247:
			case 273:
			case 277:
				return set[40];
			case 211:
				{
					BitArray a = new BitArray(239);
					a.Set(22, true);
					a.Set(143, true);
					return a;
				}
			case 217:
				return set[41];
			case 220:
			case 282:
				return set[42];
			case 221:
			case 283:
				return set[43];
			case 226:
				{
					BitArray a = new BitArray(239);
					a.Set(22, true);
					a.Set(70, true);
					return a;
				}
			case 228:
				{
					BitArray a = new BitArray(239);
					a.Set(133, true);
					a.Set(143, true);
					a.Set(146, true);
					return a;
				}
			case 237:
			case 238:
				return set[44];
			case 241:
				{
					BitArray a = new BitArray(239);
					a.Set(64, true);
					a.Set(104, true);
					return a;
				}
			case 246:
				return set[45];
			case 253:
			case 550:
			case 672:
			case 686:
			case 694:
				{
					BitArray a = new BitArray(239);
					a.Set(127, true);
					a.Set(210, true);
					return a;
				}
			case 255:
			case 256:
			case 444:
			case 445:
			case 519:
			case 520:
			case 526:
			case 527:
			case 625:
			case 626:
			case 628:
			case 629:
			case 640:
			case 641:
			case 665:
			case 666:
			case 682:
			case 683:
				return set[46];
			case 258:
			case 259:
				return set[47];
			case 265:
			case 266:
				return set[48];
			case 267:
				return set[49];
			case 270:
				return set[50];
			case 271:
			case 272:
			case 385:
				return set[51];
			case 274:
			case 279:
			case 369:
			case 661:
			case 662:
			case 664:
			case 705:
			case 722:
			case 723:
			case 725:
			case 734:
			case 735:
			case 737:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(22, true);
					return a;
				}
			case 276:
				{
					BitArray a = new BitArray(239);
					a.Set(226, true);
					return a;
				}
			case 284:
			case 285:
				return set[52];
			case 289:
			case 331:
			case 346:
				return set[53];
			case 292:
			case 293:
			case 313:
			case 314:
			case 328:
			case 329:
			case 343:
			case 344:
				return set[54];
			case 294:
			case 386:
			case 389:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(111, true);
					return a;
				}
			case 295:
				{
					BitArray a = new BitArray(239);
					a.Set(108, true);
					a.Set(124, true);
					a.Set(231, true);
					return a;
				}
			case 296:
				return set[55];
			case 297:
			case 316:
				return set[56];
			case 298:
				{
					BitArray a = new BitArray(239);
					a.Set(5, true);
					return a;
				}
			case 301:
				{
					BitArray a = new BitArray(239);
					a.Set(75, true);
					a.Set(113, true);
					a.Set(123, true);
					return a;
				}
			case 305:
			case 306:
				return set[57];
			case 307:
			case 312:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(229, true);
					return a;
				}
			case 309:
			case 310:
				return set[58];
			case 315:
				return set[59];
			case 317:
				{
					BitArray a = new BitArray(239);
					a.Set(118, true);
					return a;
				}
			case 318:
			case 319:
				return set[60];
			case 321:
			case 322:
				return set[61];
			case 333:
			case 334:
				return set[62];
			case 335:
				return set[63];
			case 337:
				{
					BitArray a = new BitArray(239);
					a.Set(20, true);
					a.Set(138, true);
					return a;
				}
			case 340:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(205, true);
					return a;
				}
			case 348:
				return set[64];
			case 349:
			case 353:
				{
					BitArray a = new BitArray(239);
					a.Set(152, true);
					return a;
				}
			case 350:
				return set[65];
			case 358:
			case 359:
				return set[66];
			case 362:
				{
					BitArray a = new BitArray(239);
					a.Set(74, true);
					a.Set(113, true);
					return a;
				}
			case 364:
			case 365:
				return set[67];
			case 367:
			case 368:
				return set[68];
			case 370:
			case 372:
				return set[69];
			case 375:
			case 381:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(214, true);
					return a;
				}
			case 378:
				{
					BitArray a = new BitArray(239);
					a.Set(111, true);
					a.Set(112, true);
					a.Set(113, true);
					return a;
				}
			case 382:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(135, true);
					return a;
				}
			case 383:
			case 384:
			case 447:
			case 448:
				return set[70];
			case 387:
			case 388:
			case 390:
			case 391:
				return set[71];
			case 393:
				return set[72];
			case 399:
				{
					BitArray a = new BitArray(239);
					a.Set(211, true);
					a.Set(233, true);
					return a;
				}
			case 401:
			case 402:
			case 411:
			case 412:
				return set[73];
			case 403:
			case 413:
				return set[74];
			case 404:
				return set[75];
			case 405:
			case 409:
				return set[76];
			case 407:
			case 408:
			case 712:
			case 713:
				return set[77];
			case 422:
			case 424:
			case 425:
			case 589:
			case 649:
				return set[78];
			case 426:
			case 427:
				return set[79];
			case 428:
			case 429:
				return set[80];
			case 430:
			case 434:
				{
					BitArray a = new BitArray(239);
					a.Set(20, true);
					a.Set(22, true);
					a.Set(38, true);
					return a;
				}
			case 438:
			case 442:
				return set[81];
			case 439:
			case 440:
				return set[82];
			case 441:
				{
					BitArray a = new BitArray(239);
					a.Set(21, true);
					return a;
				}
			case 451:
				return set[83];
			case 453:
			case 466:
				return set[84];
			case 454:
			case 467:
				return set[85];
			case 455:
			case 456:
				{
					BitArray a = new BitArray(239);
					a.Set(10, true);
					return a;
				}
			case 461:
				{
					BitArray a = new BitArray(239);
					a.Set(12, true);
					return a;
				}
			case 463:
				{
					BitArray a = new BitArray(239);
					a.Set(13, true);
					return a;
				}
			case 464:
				return set[86];
			case 465:
				return set[87];
			case 477:
				return set[88];
			case 486:
				return set[89];
			case 488:
			case 489:
			case 557:
			case 558:
			case 700:
			case 701:
				return set[90];
			case 491:
			case 492:
			case 493:
			case 498:
			case 499:
			case 560:
			case 703:
			case 730:
			case 739:
				return set[91];
			case 494:
			case 500:
			case 509:
				return set[92];
			case 495:
			case 496:
			case 501:
			case 502:
				{
					BitArray a = new BitArray(239);
					a.Set(22, true);
					a.Set(38, true);
					a.Set(63, true);
					return a;
				}
			case 503:
			case 505:
			case 510:
				return set[93];
			case 506:
			case 508:
				return set[94];
			case 515:
			case 534:
			case 535:
			case 591:
			case 679:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(63, true);
					return a;
				}
			case 516:
			case 517:
			case 595:
			case 596:
				return set[95];
			case 531:
			case 532:
			case 539:
				{
					BitArray a = new BitArray(239);
					a.Set(115, true);
					return a;
				}
			case 537:
			case 538:
				return set[96];
			case 542:
			case 543:
				return set[97];
			case 544:
			case 545:
			case 604:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(20, true);
					a.Set(21, true);
					return a;
				}
			case 549:
				{
					BitArray a = new BitArray(239);
					a.Set(103, true);
					return a;
				}
			case 561:
			case 562:
			case 574:
				{
					BitArray a = new BitArray(239);
					a.Set(84, true);
					a.Set(155, true);
					a.Set(209, true);
					return a;
				}
			case 567:
			case 568:
				return set[98];
			case 569:
			case 570:
				return set[99];
			case 571:
			case 572:
			case 583:
				return set[100];
			case 577:
			case 578:
				return set[101];
			case 579:
			case 580:
			case 719:
				return set[102];
			case 581:
				return set[103];
			case 582:
				return set[104];
			case 584:
			case 594:
				{
					BitArray a = new BitArray(239);
					a.Set(172, true);
					return a;
				}
			case 600:
			case 601:
				return set[105];
			case 602:
				return set[106];
			case 603:
			case 639:
				return set[107];
			case 606:
			case 607:
			case 608:
			case 631:
				return set[108];
			case 609:
			case 613:
			case 623:
				{
					BitArray a = new BitArray(239);
					a.Set(128, true);
					a.Set(198, true);
					return a;
				}
			case 615:
				return set[109];
			case 618:
				return set[110];
			case 619:
				return set[111];
			case 634:
			case 635:
			case 637:
			case 711:
			case 714:
				return set[112];
			case 638:
				return set[113];
			case 644:
			case 646:
			case 655:
				{
					BitArray a = new BitArray(239);
					a.Set(119, true);
					return a;
				}
			case 645:
				return set[114];
			case 647:
				return set[115];
			case 653:
				{
					BitArray a = new BitArray(239);
					a.Set(56, true);
					a.Set(189, true);
					a.Set(193, true);
					return a;
				}
			case 657:
			case 658:
				return set[116];
			case 659:
			case 669:
				{
					BitArray a = new BitArray(239);
					a.Set(1, true);
					a.Set(21, true);
					a.Set(136, true);
					return a;
				}
			case 670:
				{
					BitArray a = new BitArray(239);
					a.Set(101, true);
					return a;
				}
			case 671:
				return set[117];
			case 674:
			case 675:
				{
					BitArray a = new BitArray(239);
					a.Set(149, true);
					return a;
				}
			case 676:
			case 685:
			case 751:
				{
					BitArray a = new BitArray(239);
					a.Set(3, true);
					return a;
				}
			case 677:
				return set[118];
			case 689:
			case 690:
				return set[119];
			case 691:
			case 699:
				return set[120];
			case 695:
				return set[121];
			case 704:
			case 706:
				return set[122];
			case 707:
			case 718:
				return set[123];
			case 708:
			case 709:
				return set[124];
			case 710:
				return set[125];
			case 720:
				{
					BitArray a = new BitArray(239);
					a.Set(136, true);
					return a;
				}
			case 726:
				{
					BitArray a = new BitArray(239);
					a.Set(140, true);
					return a;
				}
			case 729:
			case 738:
				{
					BitArray a = new BitArray(239);
					a.Set(169, true);
					return a;
				}
			case 743:
				return set[126];
			case 744:
				{
					BitArray a = new BitArray(239);
					a.Set(160, true);
					return a;
				}
			case 745:
				{
					BitArray a = new BitArray(239);
					a.Set(137, true);
					return a;
				}
			case 746:
			case 747:
				return set[127];
			case 752:
				{
					BitArray a = new BitArray(239);
					a.Set(11, true);
					return a;
				}
			case 753:
				return set[128];
			case 755:
				{
					BitArray a = new BitArray(239);
					a.Set(173, true);
					return a;
				}
			case 756:
				return set[129];
			case 757:
				{
					BitArray a = new BitArray(239);
					a.Set(67, true);
					a.Set(213, true);
					return a;
				}
			case 758:
				return set[130];
			default: throw new InvalidOperationException();
		}
	}

	const bool T = true;
	const bool x = false;

	int currentState = 0;

	readonly Stack<int> stateStack = new Stack<int>();
	bool wasQualifierTokenAtStart = false;
	bool nextTokenIsPotentialStartOfExpression = false;
	bool readXmlIdentifier = false;
	bool identifierExpected = false;
	bool nextTokenIsStartOfImportsOrAccessExpression = false;
	bool isMissingModifier = false;
	bool isAlreadyInExpr = false;
	bool wasNormalAttribute = false;
	int activeArgument = 0;
	List<Token> errors = new List<Token>();
	
	public ExpressionFinder()
	{
		stateStack.Push(-1); // required so that we don't crash when leaving the root production
	}

	void Expect(int expectedKind, Token la)
	{
		if (la.kind != expectedKind) {
			Error(la);
			output.AppendLine("expected: " + expectedKind);
			//Console.WriteLine("expected: " + expectedKind);
		}
	}
	
	void Error(Token la) 
	{
		output.AppendLine("not expected: " + la);
		//Console.WriteLine("not expected: " + la);
		errors.Add(la);
	}
	
	Token t;
	
	public void InformToken(Token la) 
	{
		switchlbl: switch (currentState) {
			case 0: {
				PushContext(Context.Global, la, t);
				goto case 1;
			}
			case 1: {
				if (la == null) { currentState = 1; break; }
				if (la.kind == 173) {
					stateStack.Push(1);
					goto case 755;
				} else {
					goto case 2;
				}
			}
			case 2: {
				if (la == null) { currentState = 2; break; }
				if (la.kind == 137) {
					stateStack.Push(2);
					goto case 745;
				} else {
					goto case 3;
				}
			}
			case 3: {
				if (la == null) { currentState = 3; break; }
				if (la.kind == 40) {
					stateStack.Push(3);
					goto case 435;
				} else {
					goto case 4;
				}
			}
			case 4: {
				if (la == null) { currentState = 4; break; }
				if (set[3].Get(la.kind)) {
					stateStack.Push(4);
					goto case 5;
				} else {
					PopContext();
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 5: {
				if (la == null) { currentState = 5; break; }
				if (la.kind == 160) {
					currentState = 741;
					break;
				} else {
					if (set[4].Get(la.kind)) {
						goto case 7;
					} else {
						goto case 6;
					}
				}
			}
			case 6: {
				Error(la);
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 7: {
				if (la == null) { currentState = 7; break; }
				if (la.kind == 40) {
					stateStack.Push(7);
					goto case 435;
				} else {
					goto case 8;
				}
			}
			case 8: {
				if (la == null) { currentState = 8; break; }
				if (set[131].Get(la.kind)) {
					currentState = 8;
					break;
				} else {
					if (la.kind == 84 || la.kind == 155 || la.kind == 209) {
						goto case 561;
					} else {
						if (la.kind == 103) {
							currentState = 550;
							break;
						} else {
							if (la.kind == 115) {
								goto case 531;
							} else {
								if (la.kind == 142) {
									goto case 9;
								} else {
									goto case 6;
								}
							}
						}
					}
				}
			}
			case 9: {
				PushContext(Context.TypeDeclaration, la, t);
				goto case 10;
			}
			case 10: {
				if (la == null) { currentState = 10; break; }
				Expect(142, la); // "Interface"
				currentState = 11;
				break;
			}
			case 11: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(12);
				goto case 206;
			}
			case 12: {
				PopContext();
				goto case 13;
			}
			case 13: {
				if (la == null) { currentState = 13; break; }
				if (la.kind == 37) {
					currentState = 738;
					break;
				} else {
					goto case 14;
				}
			}
			case 14: {
				stateStack.Push(15);
				goto case 23;
			}
			case 15: {
				isMissingModifier = true;
				goto case 16;
			}
			case 16: {
				if (la == null) { currentState = 16; break; }
				if (la.kind == 140) {
					currentState = 733;
					break;
				} else {
					goto case 17;
				}
			}
			case 17: {
				isMissingModifier = true;
				goto case 18;
			}
			case 18: {
				if (la == null) { currentState = 18; break; }
				if (set[10].Get(la.kind)) {
					goto case 26;
				} else {
					isMissingModifier = false;
					goto case 19;
				}
			}
			case 19: {
				if (la == null) { currentState = 19; break; }
				Expect(113, la); // "End"
				currentState = 20;
				break;
			}
			case 20: {
				if (la == null) { currentState = 20; break; }
				Expect(142, la); // "Interface"
				currentState = 21;
				break;
			}
			case 21: {
				stateStack.Push(22);
				goto case 23;
			}
			case 22: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 23: {
				if (la != null) CurrentBlock.lastExpressionStart = la.Location;
				goto case 24;
			}
			case 24: {
				if (la == null) { currentState = 24; break; }
				if (la.kind == 1) {
					goto case 25;
				} else {
					if (la.kind == 21) {
						currentState = stateStack.Pop();
						break;
					} else {
						goto case 6;
					}
				}
			}
			case 25: {
				if (la == null) { currentState = 25; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 26: {
				isMissingModifier = true;
				goto case 27;
			}
			case 27: {
				if (la == null) { currentState = 27; break; }
				if (la.kind == 40) {
					stateStack.Push(26);
					goto case 435;
				} else {
					isMissingModifier = true;
					goto case 28;
				}
			}
			case 28: {
				if (la == null) { currentState = 28; break; }
				if (set[132].Get(la.kind)) {
					currentState = 732;
					break;
				} else {
					isMissingModifier = false;
					goto case 29;
				}
			}
			case 29: {
				if (la == null) { currentState = 29; break; }
				if (la.kind == 84 || la.kind == 155 || la.kind == 209) {
					stateStack.Push(17);
					goto case 561;
				} else {
					if (la.kind == 103) {
						stateStack.Push(17);
						goto case 549;
					} else {
						if (la.kind == 115) {
							stateStack.Push(17);
							goto case 531;
						} else {
							if (la.kind == 142) {
								stateStack.Push(17);
								goto case 9;
							} else {
								if (set[13].Get(la.kind)) {
									stateStack.Push(17);
									goto case 30;
								} else {
									Error(la);
									goto case 17;
								}
							}
						}
					}
				}
			}
			case 30: {
				if (la == null) { currentState = 30; break; }
				if (la.kind == 119) {
					currentState = 522;
					break;
				} else {
					if (la.kind == 186) {
						currentState = 512;
						break;
					} else {
						if (la.kind == 127 || la.kind == 210) {
							currentState = 31;
							break;
						} else {
							goto case 6;
						}
					}
				}
			}
			case 31: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				goto case 32;
			}
			case 32: {
				if (la == null) { currentState = 32; break; }
				currentState = 33;
				break;
			}
			case 33: {
				PopContext();
				goto case 34;
			}
			case 34: {
				if (la == null) { currentState = 34; break; }
				if (la.kind == 37) {
					currentState = 488;
					break;
				} else {
					if (la.kind == 63) {
						currentState = 35;
						break;
					} else {
						goto case 23;
					}
				}
			}
			case 35: {
				PushContext(Context.Type, la, t);
				stateStack.Push(36);
				goto case 37;
			}
			case 36: {
				PopContext();
				goto case 23;
			}
			case 37: {
				if (la == null) { currentState = 37; break; }
				if (la.kind == 130) {
					currentState = 38;
					break;
				} else {
					if (set[6].Get(la.kind)) {
						currentState = 38;
						break;
					} else {
						if (set[133].Get(la.kind)) {
							currentState = 38;
							break;
						} else {
							if (la.kind == 33) {
								currentState = 38;
								break;
							} else {
								Error(la);
								goto case 38;
							}
						}
					}
				}
			}
			case 38: {
				if (la == null) { currentState = 38; break; }
				if (la.kind == 37) {
					stateStack.Push(38);
					goto case 42;
				} else {
					goto case 39;
				}
			}
			case 39: {
				if (la == null) { currentState = 39; break; }
				if (la.kind == 26) {
					currentState = 40;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 40: {
				stateStack.Push(41);
				goto case 102;
			}
			case 41: {
				if (la == null) { currentState = 41; break; }
				if (la.kind == 37) {
					stateStack.Push(41);
					goto case 42;
				} else {
					goto case 39;
				}
			}
			case 42: {
				if (la == null) { currentState = 42; break; }
				Expect(37, la); // "("
				currentState = 43;
				break;
			}
			case 43: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 44;
			}
			case 44: {
				if (la == null) { currentState = 44; break; }
				if (la.kind == 169) {
					currentState = 486;
					break;
				} else {
					if (set[21].Get(la.kind)) {
						goto case 46;
					} else {
						Error(la);
						goto case 45;
					}
				}
			}
			case 45: {
				if (la == null) { currentState = 45; break; }
				Expect(38, la); // ")"
				currentState = stateStack.Pop();
				break;
			}
			case 46: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 47;
			}
			case 47: {
				if (la == null) { currentState = 47; break; }
				if (set[22].Get(la.kind)) {
					stateStack.Push(45);
					goto case 48;
				} else {
					goto case 45;
				}
			}
			case 48: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 49;
			}
			case 49: {
				if (la == null) { currentState = 49; break; }
				if (set[23].Get(la.kind)) {
					activeArgument = 0;
					goto case 482;
				} else {
					if (la.kind == 22) {
						activeArgument = 0;
						goto case 50;
					} else {
						goto case 6;
					}
				}
			}
			case 50: {
				if (la == null) { currentState = 50; break; }
				Expect(22, la); // ","
				currentState = 51;
				break;
			}
			case 51: {
				activeArgument++;
				goto case 52;
			}
			case 52: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 53;
			}
			case 53: {
				if (la == null) { currentState = 53; break; }
				if (set[23].Get(la.kind)) {
					stateStack.Push(54);
					goto case 56;
				} else {
					goto case 54;
				}
			}
			case 54: {
				if (la == null) { currentState = 54; break; }
				if (la.kind == 22) {
					currentState = 55;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 55: {
				activeArgument++;
				goto case 52;
			}
			case 56: {
				PushContext(Context.Expression, la, t);
				goto case 57;
			}
			case 57: {
				stateStack.Push(58);
				goto case 76;
			}
			case 58: {
				if (la == null) { currentState = 58; break; }
				if (set[25].Get(la.kind)) {
					stateStack.Push(57);
					goto case 59;
				} else {
					PopContext();
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 59: {
				if (la == null) { currentState = 59; break; }
				if (la.kind == 31) {
					currentState = stateStack.Pop();
					break;
				} else {
					if (la.kind == 30) {
						currentState = stateStack.Pop();
						break;
					} else {
						if (la.kind == 34) {
							currentState = stateStack.Pop();
							break;
						} else {
							if (la.kind == 25) {
								currentState = stateStack.Pop();
								break;
							} else {
								if (la.kind == 24) {
									currentState = stateStack.Pop();
									break;
								} else {
									if (la.kind == 32) {
										currentState = stateStack.Pop();
										break;
									} else {
										if (la.kind == 154) {
											goto case 75;
										} else {
											if (la.kind == 20) {
												goto case 74;
											} else {
												if (la.kind == 41) {
													goto case 73;
												} else {
													if (la.kind == 40) {
														goto case 72;
													} else {
														if (la.kind == 39) {
															currentState = 71;
															break;
														} else {
															if (la.kind == 43) {
																goto case 70;
															} else {
																if (la.kind == 42) {
																	goto case 69;
																} else {
																	if (la.kind == 150) {
																		goto case 68;
																	} else {
																		if (la.kind == 23) {
																			currentState = stateStack.Pop();
																			break;
																		} else {
																			if (la.kind == 60) {
																				goto case 67;
																			} else {
																				if (la.kind == 61) {
																					goto case 66;
																				} else {
																					if (la.kind == 175) {
																						goto case 65;
																					} else {
																						if (la.kind == 177) {
																							goto case 64;
																						} else {
																							if (la.kind == 236) {
																								goto case 63;
																							} else {
																								if (la.kind == 44) {
																									currentState = stateStack.Pop();
																									break;
																								} else {
																									if (la.kind == 45) {
																										currentState = stateStack.Pop();
																										break;
																									} else {
																										if (la.kind == 144) {
																											goto case 62;
																										} else {
																											if (la.kind == 145) {
																												goto case 61;
																											} else {
																												if (la.kind == 47) {
																													currentState = stateStack.Pop();
																													break;
																												} else {
																													if (la.kind == 49) {
																														currentState = stateStack.Pop();
																														break;
																													} else {
																														if (la.kind == 50) {
																															currentState = stateStack.Pop();
																															break;
																														} else {
																															if (la.kind == 51) {
																																currentState = stateStack.Pop();
																																break;
																															} else {
																																if (la.kind == 46) {
																																	currentState = stateStack.Pop();
																																	break;
																																} else {
																																	if (la.kind == 48) {
																																		currentState = stateStack.Pop();
																																		break;
																																	} else {
																																		if (la.kind == 54) {
																																			currentState = stateStack.Pop();
																																			break;
																																		} else {
																																			if (la.kind == 52) {
																																				currentState = stateStack.Pop();
																																				break;
																																			} else {
																																				if (la.kind == 53) {
																																					currentState = stateStack.Pop();
																																					break;
																																				} else {
																																					if (la.kind == 216) {
																																						goto case 60;
																																					} else {
																																						if (la.kind == 55) {
																																							currentState = stateStack.Pop();
																																							break;
																																						} else {
																																							goto case 6;
																																						}
																																					}
																																				}
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 60: {
				if (la == null) { currentState = 60; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 61: {
				if (la == null) { currentState = 61; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 62: {
				if (la == null) { currentState = 62; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 63: {
				if (la == null) { currentState = 63; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 64: {
				if (la == null) { currentState = 64; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 65: {
				if (la == null) { currentState = 65; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 66: {
				if (la == null) { currentState = 66; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 67: {
				if (la == null) { currentState = 67; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 68: {
				if (la == null) { currentState = 68; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 69: {
				if (la == null) { currentState = 69; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 70: {
				if (la == null) { currentState = 70; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 71: {
				wasNormalAttribute = false;
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 72: {
				if (la == null) { currentState = 72; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 73: {
				if (la == null) { currentState = 73; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 74: {
				if (la == null) { currentState = 74; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 75: {
				if (la == null) { currentState = 75; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 76: {
				PushContext(Context.Expression, la, t);
				goto case 77;
			}
			case 77: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 78;
			}
			case 78: {
				if (la == null) { currentState = 78; break; }
				if (set[134].Get(la.kind)) {
					currentState = 77;
					break;
				} else {
					if (set[35].Get(la.kind)) {
						stateStack.Push(160);
						goto case 174;
					} else {
						if (la.kind == 220) {
							currentState = 156;
							break;
						} else {
							if (la.kind == 162) {
								stateStack.Push(79);
								goto case 86;
							} else {
								if (la.kind == 35) {
									stateStack.Push(79);
									goto case 80;
								} else {
									Error(la);
									goto case 79;
								}
							}
						}
					}
				}
			}
			case 79: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 80: {
				if (la == null) { currentState = 80; break; }
				Expect(35, la); // "{"
				currentState = 81;
				break;
			}
			case 81: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 82;
			}
			case 82: {
				if (la == null) { currentState = 82; break; }
				if (set[23].Get(la.kind)) {
					goto case 84;
				} else {
					goto case 83;
				}
			}
			case 83: {
				if (la == null) { currentState = 83; break; }
				Expect(36, la); // "}"
				currentState = stateStack.Pop();
				break;
			}
			case 84: {
				stateStack.Push(85);
				goto case 56;
			}
			case 85: {
				if (la == null) { currentState = 85; break; }
				if (la.kind == 22) {
					currentState = 84;
					break;
				} else {
					goto case 83;
				}
			}
			case 86: {
				if (la == null) { currentState = 86; break; }
				Expect(162, la); // "New"
				currentState = 87;
				break;
			}
			case 87: {
				PushContext(Context.ObjectCreation, la, t);
				goto case 88;
			}
			case 88: {
				if (la == null) { currentState = 88; break; }
				if (set[16].Get(la.kind)) {
					stateStack.Push(147);
					goto case 37;
				} else {
					if (la.kind == 233) {
						PushContext(Context.ObjectInitializer, la, t);
						goto case 91;
					} else {
						goto case 89;
					}
				}
			}
			case 89: {
				Error(la);
				goto case 90;
			}
			case 90: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 91: {
				if (la == null) { currentState = 91; break; }
				Expect(233, la); // "With"
				currentState = 92;
				break;
			}
			case 92: {
				stateStack.Push(93);
				goto case 94;
			}
			case 93: {
				PopContext();
				goto case 90;
			}
			case 94: {
				if (la == null) { currentState = 94; break; }
				Expect(35, la); // "{"
				currentState = 95;
				break;
			}
			case 95: {
				if (la == null) { currentState = 95; break; }
				if (la.kind == 26 || la.kind == 147) {
					goto case 96;
				} else {
					goto case 83;
				}
			}
			case 96: {
				if (la == null) { currentState = 96; break; }
				if (la.kind == 147) {
					currentState = 97;
					break;
				} else {
					goto case 97;
				}
			}
			case 97: {
				if (la == null) { currentState = 97; break; }
				Expect(26, la); // "."
				currentState = 98;
				break;
			}
			case 98: {
				stateStack.Push(99);
				goto case 102;
			}
			case 99: {
				if (la == null) { currentState = 99; break; }
				Expect(20, la); // "="
				currentState = 100;
				break;
			}
			case 100: {
				stateStack.Push(101);
				goto case 56;
			}
			case 101: {
				if (la == null) { currentState = 101; break; }
				if (la.kind == 22) {
					currentState = 96;
					break;
				} else {
					goto case 83;
				}
			}
			case 102: {
				if (la == null) { currentState = 102; break; }
				if (la.kind == 2) {
					goto case 146;
				} else {
					if (la.kind == 56) {
						currentState = stateStack.Pop();
						break;
					} else {
						if (la.kind == 57) {
							currentState = stateStack.Pop();
							break;
						} else {
							if (la.kind == 58) {
								goto case 145;
							} else {
								if (la.kind == 59) {
									currentState = stateStack.Pop();
									break;
								} else {
									if (la.kind == 60) {
										goto case 67;
									} else {
										if (la.kind == 61) {
											goto case 66;
										} else {
											if (la.kind == 62) {
												goto case 144;
											} else {
												if (la.kind == 63) {
													currentState = stateStack.Pop();
													break;
												} else {
													if (la.kind == 64) {
														goto case 143;
													} else {
														if (la.kind == 65) {
															goto case 142;
														} else {
															if (la.kind == 66) {
																goto case 141;
															} else {
																if (la.kind == 67) {
																	goto case 140;
																} else {
																	if (la.kind == 68) {
																		currentState = stateStack.Pop();
																		break;
																	} else {
																		if (la.kind == 69) {
																			currentState = stateStack.Pop();
																			break;
																		} else {
																			if (la.kind == 70) {
																				goto case 139;
																			} else {
																				if (la.kind == 71) {
																					currentState = stateStack.Pop();
																					break;
																				} else {
																					if (la.kind == 72) {
																						currentState = stateStack.Pop();
																						break;
																					} else {
																						if (la.kind == 73) {
																							currentState = stateStack.Pop();
																							break;
																						} else {
																							if (la.kind == 74) {
																								currentState = stateStack.Pop();
																								break;
																							} else {
																								if (la.kind == 75) {
																									currentState = stateStack.Pop();
																									break;
																								} else {
																									if (la.kind == 76) {
																										currentState = stateStack.Pop();
																										break;
																									} else {
																										if (la.kind == 77) {
																											currentState = stateStack.Pop();
																											break;
																										} else {
																											if (la.kind == 78) {
																												currentState = stateStack.Pop();
																												break;
																											} else {
																												if (la.kind == 79) {
																													currentState = stateStack.Pop();
																													break;
																												} else {
																													if (la.kind == 80) {
																														currentState = stateStack.Pop();
																														break;
																													} else {
																														if (la.kind == 81) {
																															currentState = stateStack.Pop();
																															break;
																														} else {
																															if (la.kind == 82) {
																																currentState = stateStack.Pop();
																																break;
																															} else {
																																if (la.kind == 83) {
																																	currentState = stateStack.Pop();
																																	break;
																																} else {
																																	if (la.kind == 84) {
																																		goto case 138;
																																	} else {
																																		if (la.kind == 85) {
																																			currentState = stateStack.Pop();
																																			break;
																																		} else {
																																			if (la.kind == 86) {
																																				currentState = stateStack.Pop();
																																				break;
																																			} else {
																																				if (la.kind == 87) {
																																					goto case 137;
																																				} else {
																																					if (la.kind == 88) {
																																						currentState = stateStack.Pop();
																																						break;
																																					} else {
																																						if (la.kind == 89) {
																																							currentState = stateStack.Pop();
																																							break;
																																						} else {
																																							if (la.kind == 90) {
																																								currentState = stateStack.Pop();
																																								break;
																																							} else {
																																								if (la.kind == 91) {
																																									currentState = stateStack.Pop();
																																									break;
																																								} else {
																																									if (la.kind == 92) {
																																										currentState = stateStack.Pop();
																																										break;
																																									} else {
																																										if (la.kind == 93) {
																																											currentState = stateStack.Pop();
																																											break;
																																										} else {
																																											if (la.kind == 94) {
																																												currentState = stateStack.Pop();
																																												break;
																																											} else {
																																												if (la.kind == 95) {
																																													currentState = stateStack.Pop();
																																													break;
																																												} else {
																																													if (la.kind == 96) {
																																														currentState = stateStack.Pop();
																																														break;
																																													} else {
																																														if (la.kind == 97) {
																																															currentState = stateStack.Pop();
																																															break;
																																														} else {
																																															if (la.kind == 98) {
																																																goto case 136;
																																															} else {
																																																if (la.kind == 99) {
																																																	currentState = stateStack.Pop();
																																																	break;
																																																} else {
																																																	if (la.kind == 100) {
																																																		currentState = stateStack.Pop();
																																																		break;
																																																	} else {
																																																		if (la.kind == 101) {
																																																			currentState = stateStack.Pop();
																																																			break;
																																																		} else {
																																																			if (la.kind == 102) {
																																																				currentState = stateStack.Pop();
																																																				break;
																																																			} else {
																																																				if (la.kind == 103) {
																																																					currentState = stateStack.Pop();
																																																					break;
																																																				} else {
																																																					if (la.kind == 104) {
																																																						goto case 135;
																																																					} else {
																																																						if (la.kind == 105) {
																																																							currentState = stateStack.Pop();
																																																							break;
																																																						} else {
																																																							if (la.kind == 106) {
																																																								currentState = stateStack.Pop();
																																																								break;
																																																							} else {
																																																								if (la.kind == 107) {
																																																									goto case 134;
																																																								} else {
																																																									if (la.kind == 108) {
																																																										goto case 133;
																																																									} else {
																																																										if (la.kind == 109) {
																																																											currentState = stateStack.Pop();
																																																											break;
																																																										} else {
																																																											if (la.kind == 110) {
																																																												currentState = stateStack.Pop();
																																																												break;
																																																											} else {
																																																												if (la.kind == 111) {
																																																													currentState = stateStack.Pop();
																																																													break;
																																																												} else {
																																																													if (la.kind == 112) {
																																																														currentState = stateStack.Pop();
																																																														break;
																																																													} else {
																																																														if (la.kind == 113) {
																																																															currentState = stateStack.Pop();
																																																															break;
																																																														} else {
																																																															if (la.kind == 114) {
																																																																currentState = stateStack.Pop();
																																																																break;
																																																															} else {
																																																																if (la.kind == 115) {
																																																																	currentState = stateStack.Pop();
																																																																	break;
																																																																} else {
																																																																	if (la.kind == 116) {
																																																																		goto case 132;
																																																																	} else {
																																																																		if (la.kind == 117) {
																																																																			currentState = stateStack.Pop();
																																																																			break;
																																																																		} else {
																																																																			if (la.kind == 118) {
																																																																				currentState = stateStack.Pop();
																																																																				break;
																																																																			} else {
																																																																				if (la.kind == 119) {
																																																																					currentState = stateStack.Pop();
																																																																					break;
																																																																				} else {
																																																																					if (la.kind == 120) {
																																																																						currentState = stateStack.Pop();
																																																																						break;
																																																																					} else {
																																																																						if (la.kind == 121) {
																																																																							goto case 131;
																																																																						} else {
																																																																							if (la.kind == 122) {
																																																																								currentState = stateStack.Pop();
																																																																								break;
																																																																							} else {
																																																																								if (la.kind == 123) {
																																																																									currentState = stateStack.Pop();
																																																																									break;
																																																																								} else {
																																																																									if (la.kind == 124) {
																																																																										goto case 130;
																																																																									} else {
																																																																										if (la.kind == 125) {
																																																																											currentState = stateStack.Pop();
																																																																											break;
																																																																										} else {
																																																																											if (la.kind == 126) {
																																																																												goto case 129;
																																																																											} else {
																																																																												if (la.kind == 127) {
																																																																													goto case 128;
																																																																												} else {
																																																																													if (la.kind == 128) {
																																																																														currentState = stateStack.Pop();
																																																																														break;
																																																																													} else {
																																																																														if (la.kind == 129) {
																																																																															currentState = stateStack.Pop();
																																																																															break;
																																																																														} else {
																																																																															if (la.kind == 130) {
																																																																																currentState = stateStack.Pop();
																																																																																break;
																																																																															} else {
																																																																																if (la.kind == 131) {
																																																																																	currentState = stateStack.Pop();
																																																																																	break;
																																																																																} else {
																																																																																	if (la.kind == 132) {
																																																																																		currentState = stateStack.Pop();
																																																																																		break;
																																																																																	} else {
																																																																																		if (la.kind == 133) {
																																																																																			goto case 127;
																																																																																		} else {
																																																																																			if (la.kind == 134) {
																																																																																				currentState = stateStack.Pop();
																																																																																				break;
																																																																																			} else {
																																																																																				if (la.kind == 135) {
																																																																																					currentState = stateStack.Pop();
																																																																																					break;
																																																																																				} else {
																																																																																					if (la.kind == 136) {
																																																																																						currentState = stateStack.Pop();
																																																																																						break;
																																																																																					} else {
																																																																																						if (la.kind == 137) {
																																																																																							currentState = stateStack.Pop();
																																																																																							break;
																																																																																						} else {
																																																																																							if (la.kind == 138) {
																																																																																								currentState = stateStack.Pop();
																																																																																								break;
																																																																																							} else {
																																																																																								if (la.kind == 139) {
																																																																																									goto case 126;
																																																																																								} else {
																																																																																									if (la.kind == 140) {
																																																																																										currentState = stateStack.Pop();
																																																																																										break;
																																																																																									} else {
																																																																																										if (la.kind == 141) {
																																																																																											currentState = stateStack.Pop();
																																																																																											break;
																																																																																										} else {
																																																																																											if (la.kind == 142) {
																																																																																												currentState = stateStack.Pop();
																																																																																												break;
																																																																																											} else {
																																																																																												if (la.kind == 143) {
																																																																																													goto case 125;
																																																																																												} else {
																																																																																													if (la.kind == 144) {
																																																																																														goto case 62;
																																																																																													} else {
																																																																																														if (la.kind == 145) {
																																																																																															goto case 61;
																																																																																														} else {
																																																																																															if (la.kind == 146) {
																																																																																																goto case 124;
																																																																																															} else {
																																																																																																if (la.kind == 147) {
																																																																																																	goto case 123;
																																																																																																} else {
																																																																																																	if (la.kind == 148) {
																																																																																																		currentState = stateStack.Pop();
																																																																																																		break;
																																																																																																	} else {
																																																																																																		if (la.kind == 149) {
																																																																																																			currentState = stateStack.Pop();
																																																																																																			break;
																																																																																																		} else {
																																																																																																			if (la.kind == 150) {
																																																																																																				goto case 68;
																																																																																																			} else {
																																																																																																				if (la.kind == 151) {
																																																																																																					currentState = stateStack.Pop();
																																																																																																					break;
																																																																																																				} else {
																																																																																																					if (la.kind == 152) {
																																																																																																						currentState = stateStack.Pop();
																																																																																																						break;
																																																																																																					} else {
																																																																																																						if (la.kind == 153) {
																																																																																																							currentState = stateStack.Pop();
																																																																																																							break;
																																																																																																						} else {
																																																																																																							if (la.kind == 154) {
																																																																																																								goto case 75;
																																																																																																							} else {
																																																																																																								if (la.kind == 155) {
																																																																																																									currentState = stateStack.Pop();
																																																																																																									break;
																																																																																																								} else {
																																																																																																									if (la.kind == 156) {
																																																																																																										currentState = stateStack.Pop();
																																																																																																										break;
																																																																																																									} else {
																																																																																																										if (la.kind == 157) {
																																																																																																											currentState = stateStack.Pop();
																																																																																																											break;
																																																																																																										} else {
																																																																																																											if (la.kind == 158) {
																																																																																																												currentState = stateStack.Pop();
																																																																																																												break;
																																																																																																											} else {
																																																																																																												if (la.kind == 159) {
																																																																																																													currentState = stateStack.Pop();
																																																																																																													break;
																																																																																																												} else {
																																																																																																													if (la.kind == 160) {
																																																																																																														currentState = stateStack.Pop();
																																																																																																														break;
																																																																																																													} else {
																																																																																																														if (la.kind == 161) {
																																																																																																															currentState = stateStack.Pop();
																																																																																																															break;
																																																																																																														} else {
																																																																																																															if (la.kind == 162) {
																																																																																																																goto case 122;
																																																																																																															} else {
																																																																																																																if (la.kind == 163) {
																																																																																																																	goto case 121;
																																																																																																																} else {
																																																																																																																	if (la.kind == 164) {
																																																																																																																		currentState = stateStack.Pop();
																																																																																																																		break;
																																																																																																																	} else {
																																																																																																																		if (la.kind == 165) {
																																																																																																																			currentState = stateStack.Pop();
																																																																																																																			break;
																																																																																																																		} else {
																																																																																																																			if (la.kind == 166) {
																																																																																																																				currentState = stateStack.Pop();
																																																																																																																				break;
																																																																																																																			} else {
																																																																																																																				if (la.kind == 167) {
																																																																																																																					currentState = stateStack.Pop();
																																																																																																																					break;
																																																																																																																				} else {
																																																																																																																					if (la.kind == 168) {
																																																																																																																						currentState = stateStack.Pop();
																																																																																																																						break;
																																																																																																																					} else {
																																																																																																																						if (la.kind == 169) {
																																																																																																																							currentState = stateStack.Pop();
																																																																																																																							break;
																																																																																																																						} else {
																																																																																																																							if (la.kind == 170) {
																																																																																																																								goto case 120;
																																																																																																																							} else {
																																																																																																																								if (la.kind == 171) {
																																																																																																																									currentState = stateStack.Pop();
																																																																																																																									break;
																																																																																																																								} else {
																																																																																																																									if (la.kind == 172) {
																																																																																																																										currentState = stateStack.Pop();
																																																																																																																										break;
																																																																																																																									} else {
																																																																																																																										if (la.kind == 173) {
																																																																																																																											currentState = stateStack.Pop();
																																																																																																																											break;
																																																																																																																										} else {
																																																																																																																											if (la.kind == 174) {
																																																																																																																												currentState = stateStack.Pop();
																																																																																																																												break;
																																																																																																																											} else {
																																																																																																																												if (la.kind == 175) {
																																																																																																																													goto case 65;
																																																																																																																												} else {
																																																																																																																													if (la.kind == 176) {
																																																																																																																														goto case 119;
																																																																																																																													} else {
																																																																																																																														if (la.kind == 177) {
																																																																																																																															goto case 64;
																																																																																																																														} else {
																																																																																																																															if (la.kind == 178) {
																																																																																																																																currentState = stateStack.Pop();
																																																																																																																																break;
																																																																																																																															} else {
																																																																																																																																if (la.kind == 179) {
																																																																																																																																	currentState = stateStack.Pop();
																																																																																																																																	break;
																																																																																																																																} else {
																																																																																																																																	if (la.kind == 180) {
																																																																																																																																		currentState = stateStack.Pop();
																																																																																																																																		break;
																																																																																																																																	} else {
																																																																																																																																		if (la.kind == 181) {
																																																																																																																																			currentState = stateStack.Pop();
																																																																																																																																			break;
																																																																																																																																		} else {
																																																																																																																																			if (la.kind == 182) {
																																																																																																																																				currentState = stateStack.Pop();
																																																																																																																																				break;
																																																																																																																																			} else {
																																																																																																																																				if (la.kind == 183) {
																																																																																																																																					currentState = stateStack.Pop();
																																																																																																																																					break;
																																																																																																																																				} else {
																																																																																																																																					if (la.kind == 184) {
																																																																																																																																						goto case 118;
																																																																																																																																					} else {
																																																																																																																																						if (la.kind == 185) {
																																																																																																																																							currentState = stateStack.Pop();
																																																																																																																																							break;
																																																																																																																																						} else {
																																																																																																																																							if (la.kind == 186) {
																																																																																																																																								goto case 117;
																																																																																																																																							} else {
																																																																																																																																								if (la.kind == 187) {
																																																																																																																																									currentState = stateStack.Pop();
																																																																																																																																									break;
																																																																																																																																								} else {
																																																																																																																																									if (la.kind == 188) {
																																																																																																																																										currentState = stateStack.Pop();
																																																																																																																																										break;
																																																																																																																																									} else {
																																																																																																																																										if (la.kind == 189) {
																																																																																																																																											currentState = stateStack.Pop();
																																																																																																																																											break;
																																																																																																																																										} else {
																																																																																																																																											if (la.kind == 190) {
																																																																																																																																												currentState = stateStack.Pop();
																																																																																																																																												break;
																																																																																																																																											} else {
																																																																																																																																												if (la.kind == 191) {
																																																																																																																																													currentState = stateStack.Pop();
																																																																																																																																													break;
																																																																																																																																												} else {
																																																																																																																																													if (la.kind == 192) {
																																																																																																																																														currentState = stateStack.Pop();
																																																																																																																																														break;
																																																																																																																																													} else {
																																																																																																																																														if (la.kind == 193) {
																																																																																																																																															currentState = stateStack.Pop();
																																																																																																																																															break;
																																																																																																																																														} else {
																																																																																																																																															if (la.kind == 194) {
																																																																																																																																																currentState = stateStack.Pop();
																																																																																																																																																break;
																																																																																																																																															} else {
																																																																																																																																																if (la.kind == 195) {
																																																																																																																																																	currentState = stateStack.Pop();
																																																																																																																																																	break;
																																																																																																																																																} else {
																																																																																																																																																	if (la.kind == 196) {
																																																																																																																																																		currentState = stateStack.Pop();
																																																																																																																																																		break;
																																																																																																																																																	} else {
																																																																																																																																																		if (la.kind == 197) {
																																																																																																																																																			goto case 116;
																																																																																																																																																		} else {
																																																																																																																																																			if (la.kind == 198) {
																																																																																																																																																				currentState = stateStack.Pop();
																																																																																																																																																				break;
																																																																																																																																																			} else {
																																																																																																																																																				if (la.kind == 199) {
																																																																																																																																																					currentState = stateStack.Pop();
																																																																																																																																																					break;
																																																																																																																																																				} else {
																																																																																																																																																					if (la.kind == 200) {
																																																																																																																																																						currentState = stateStack.Pop();
																																																																																																																																																						break;
																																																																																																																																																					} else {
																																																																																																																																																						if (la.kind == 201) {
																																																																																																																																																							currentState = stateStack.Pop();
																																																																																																																																																							break;
																																																																																																																																																						} else {
																																																																																																																																																							if (la.kind == 202) {
																																																																																																																																																								currentState = stateStack.Pop();
																																																																																																																																																								break;
																																																																																																																																																							} else {
																																																																																																																																																								if (la.kind == 203) {
																																																																																																																																																									goto case 115;
																																																																																																																																																								} else {
																																																																																																																																																									if (la.kind == 204) {
																																																																																																																																																										currentState = stateStack.Pop();
																																																																																																																																																										break;
																																																																																																																																																									} else {
																																																																																																																																																										if (la.kind == 205) {
																																																																																																																																																											currentState = stateStack.Pop();
																																																																																																																																																											break;
																																																																																																																																																										} else {
																																																																																																																																																											if (la.kind == 206) {
																																																																																																																																																												goto case 114;
																																																																																																																																																											} else {
																																																																																																																																																												if (la.kind == 207) {
																																																																																																																																																													currentState = stateStack.Pop();
																																																																																																																																																													break;
																																																																																																																																																												} else {
																																																																																																																																																													if (la.kind == 208) {
																																																																																																																																																														currentState = stateStack.Pop();
																																																																																																																																																														break;
																																																																																																																																																													} else {
																																																																																																																																																														if (la.kind == 209) {
																																																																																																																																																															goto case 113;
																																																																																																																																																														} else {
																																																																																																																																																															if (la.kind == 210) {
																																																																																																																																																																goto case 112;
																																																																																																																																																															} else {
																																																																																																																																																																if (la.kind == 211) {
																																																																																																																																																																	goto case 111;
																																																																																																																																																																} else {
																																																																																																																																																																	if (la.kind == 212) {
																																																																																																																																																																		goto case 110;
																																																																																																																																																																	} else {
																																																																																																																																																																		if (la.kind == 213) {
																																																																																																																																																																			goto case 109;
																																																																																																																																																																		} else {
																																																																																																																																																																			if (la.kind == 214) {
																																																																																																																																																																				currentState = stateStack.Pop();
																																																																																																																																																																				break;
																																																																																																																																																																			} else {
																																																																																																																																																																				if (la.kind == 215) {
																																																																																																																																																																					currentState = stateStack.Pop();
																																																																																																																																																																					break;
																																																																																																																																																																				} else {
																																																																																																																																																																					if (la.kind == 216) {
																																																																																																																																																																						goto case 60;
																																																																																																																																																																					} else {
																																																																																																																																																																						if (la.kind == 217) {
																																																																																																																																																																							currentState = stateStack.Pop();
																																																																																																																																																																							break;
																																																																																																																																																																						} else {
																																																																																																																																																																							if (la.kind == 218) {
																																																																																																																																																																								goto case 108;
																																																																																																																																																																							} else {
																																																																																																																																																																								if (la.kind == 219) {
																																																																																																																																																																									currentState = stateStack.Pop();
																																																																																																																																																																									break;
																																																																																																																																																																								} else {
																																																																																																																																																																									if (la.kind == 220) {
																																																																																																																																																																										currentState = stateStack.Pop();
																																																																																																																																																																										break;
																																																																																																																																																																									} else {
																																																																																																																																																																										if (la.kind == 221) {
																																																																																																																																																																											currentState = stateStack.Pop();
																																																																																																																																																																											break;
																																																																																																																																																																										} else {
																																																																																																																																																																											if (la.kind == 222) {
																																																																																																																																																																												currentState = stateStack.Pop();
																																																																																																																																																																												break;
																																																																																																																																																																											} else {
																																																																																																																																																																												if (la.kind == 223) {
																																																																																																																																																																													goto case 107;
																																																																																																																																																																												} else {
																																																																																																																																																																													if (la.kind == 224) {
																																																																																																																																																																														goto case 106;
																																																																																																																																																																													} else {
																																																																																																																																																																														if (la.kind == 225) {
																																																																																																																																																																															currentState = stateStack.Pop();
																																																																																																																																																																															break;
																																																																																																																																																																														} else {
																																																																																																																																																																															if (la.kind == 226) {
																																																																																																																																																																																currentState = stateStack.Pop();
																																																																																																																																																																																break;
																																																																																																																																																																															} else {
																																																																																																																																																																																if (la.kind == 227) {
																																																																																																																																																																																	currentState = stateStack.Pop();
																																																																																																																																																																																	break;
																																																																																																																																																																																} else {
																																																																																																																																																																																	if (la.kind == 228) {
																																																																																																																																																																																		currentState = stateStack.Pop();
																																																																																																																																																																																		break;
																																																																																																																																																																																	} else {
																																																																																																																																																																																		if (la.kind == 229) {
																																																																																																																																																																																			currentState = stateStack.Pop();
																																																																																																																																																																																			break;
																																																																																																																																																																																		} else {
																																																																																																																																																																																			if (la.kind == 230) {
																																																																																																																																																																																				goto case 105;
																																																																																																																																																																																			} else {
																																																																																																																																																																																				if (la.kind == 231) {
																																																																																																																																																																																					goto case 104;
																																																																																																																																																																																				} else {
																																																																																																																																																																																					if (la.kind == 232) {
																																																																																																																																																																																						currentState = stateStack.Pop();
																																																																																																																																																																																						break;
																																																																																																																																																																																					} else {
																																																																																																																																																																																						if (la.kind == 233) {
																																																																																																																																																																																							goto case 103;
																																																																																																																																																																																						} else {
																																																																																																																																																																																							if (la.kind == 234) {
																																																																																																																																																																																								currentState = stateStack.Pop();
																																																																																																																																																																																								break;
																																																																																																																																																																																							} else {
																																																																																																																																																																																								if (la.kind == 235) {
																																																																																																																																																																																									currentState = stateStack.Pop();
																																																																																																																																																																																									break;
																																																																																																																																																																																								} else {
																																																																																																																																																																																									if (la.kind == 236) {
																																																																																																																																																																																										goto case 63;
																																																																																																																																																																																									} else {
																																																																																																																																																																																										if (la.kind == 237) {
																																																																																																																																																																																											currentState = stateStack.Pop();
																																																																																																																																																																																											break;
																																																																																																																																																																																										} else {
																																																																																																																																																																																											goto case 6;
																																																																																																																																																																																										}
																																																																																																																																																																																									}
																																																																																																																																																																																								}
																																																																																																																																																																																							}
																																																																																																																																																																																						}
																																																																																																																																																																																					}
																																																																																																																																																																																				}
																																																																																																																																																																																			}
																																																																																																																																																																																		}
																																																																																																																																																																																	}
																																																																																																																																																																																}
																																																																																																																																																																															}
																																																																																																																																																																														}
																																																																																																																																																																													}
																																																																																																																																																																												}
																																																																																																																																																																											}
																																																																																																																																																																										}
																																																																																																																																																																									}
																																																																																																																																																																								}
																																																																																																																																																																							}
																																																																																																																																																																						}
																																																																																																																																																																					}
																																																																																																																																																																				}
																																																																																																																																																																			}
																																																																																																																																																																		}
																																																																																																																																																																	}
																																																																																																																																																																}
																																																																																																																																																															}
																																																																																																																																																														}
																																																																																																																																																													}
																																																																																																																																																												}
																																																																																																																																																											}
																																																																																																																																																										}
																																																																																																																																																									}
																																																																																																																																																								}
																																																																																																																																																							}
																																																																																																																																																						}
																																																																																																																																																					}
																																																																																																																																																				}
																																																																																																																																																			}
																																																																																																																																																		}
																																																																																																																																																	}
																																																																																																																																																}
																																																																																																																																															}
																																																																																																																																														}
																																																																																																																																													}
																																																																																																																																												}
																																																																																																																																											}
																																																																																																																																										}
																																																																																																																																									}
																																																																																																																																								}
																																																																																																																																							}
																																																																																																																																						}
																																																																																																																																					}
																																																																																																																																				}
																																																																																																																																			}
																																																																																																																																		}
																																																																																																																																	}
																																																																																																																																}
																																																																																																																															}
																																																																																																																														}
																																																																																																																													}
																																																																																																																												}
																																																																																																																											}
																																																																																																																										}
																																																																																																																									}
																																																																																																																								}
																																																																																																																							}
																																																																																																																						}
																																																																																																																					}
																																																																																																																				}
																																																																																																																			}
																																																																																																																		}
																																																																																																																	}
																																																																																																																}
																																																																																																															}
																																																																																																														}
																																																																																																													}
																																																																																																												}
																																																																																																											}
																																																																																																										}
																																																																																																									}
																																																																																																								}
																																																																																																							}
																																																																																																						}
																																																																																																					}
																																																																																																				}
																																																																																																			}
																																																																																																		}
																																																																																																	}
																																																																																																}
																																																																																															}
																																																																																														}
																																																																																													}
																																																																																												}
																																																																																											}
																																																																																										}
																																																																																									}
																																																																																								}
																																																																																							}
																																																																																						}
																																																																																					}
																																																																																				}
																																																																																			}
																																																																																		}
																																																																																	}
																																																																																}
																																																																															}
																																																																														}
																																																																													}
																																																																												}
																																																																											}
																																																																										}
																																																																									}
																																																																								}
																																																																							}
																																																																						}
																																																																					}
																																																																				}
																																																																			}
																																																																		}
																																																																	}
																																																																}
																																																															}
																																																														}
																																																													}
																																																												}
																																																											}
																																																										}
																																																									}
																																																								}
																																																							}
																																																						}
																																																					}
																																																				}
																																																			}
																																																		}
																																																	}
																																																}
																																															}
																																														}
																																													}
																																												}
																																											}
																																										}
																																									}
																																								}
																																							}
																																						}
																																					}
																																				}
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 103: {
				if (la == null) { currentState = 103; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 104: {
				if (la == null) { currentState = 104; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 105: {
				if (la == null) { currentState = 105; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 106: {
				if (la == null) { currentState = 106; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 107: {
				if (la == null) { currentState = 107; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 108: {
				if (la == null) { currentState = 108; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 109: {
				if (la == null) { currentState = 109; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 110: {
				if (la == null) { currentState = 110; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 111: {
				if (la == null) { currentState = 111; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 112: {
				if (la == null) { currentState = 112; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 113: {
				if (la == null) { currentState = 113; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 114: {
				if (la == null) { currentState = 114; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 115: {
				if (la == null) { currentState = 115; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 116: {
				if (la == null) { currentState = 116; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 117: {
				if (la == null) { currentState = 117; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 118: {
				if (la == null) { currentState = 118; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 119: {
				if (la == null) { currentState = 119; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 120: {
				if (la == null) { currentState = 120; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 121: {
				if (la == null) { currentState = 121; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 122: {
				if (la == null) { currentState = 122; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 123: {
				if (la == null) { currentState = 123; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 124: {
				if (la == null) { currentState = 124; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 125: {
				if (la == null) { currentState = 125; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 126: {
				if (la == null) { currentState = 126; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 127: {
				if (la == null) { currentState = 127; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 128: {
				if (la == null) { currentState = 128; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 129: {
				if (la == null) { currentState = 129; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 130: {
				if (la == null) { currentState = 130; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 131: {
				if (la == null) { currentState = 131; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 132: {
				if (la == null) { currentState = 132; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 133: {
				if (la == null) { currentState = 133; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 134: {
				if (la == null) { currentState = 134; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 135: {
				if (la == null) { currentState = 135; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 136: {
				if (la == null) { currentState = 136; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 137: {
				if (la == null) { currentState = 137; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 138: {
				if (la == null) { currentState = 138; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 139: {
				if (la == null) { currentState = 139; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 140: {
				if (la == null) { currentState = 140; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 141: {
				if (la == null) { currentState = 141; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 142: {
				if (la == null) { currentState = 142; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 143: {
				if (la == null) { currentState = 143; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 144: {
				if (la == null) { currentState = 144; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 145: {
				if (la == null) { currentState = 145; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 146: {
				if (la == null) { currentState = 146; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 147: {
				if (la == null) { currentState = 147; break; }
				if (la.kind == 35 || la.kind == 126 || la.kind == 233) {
					if (la.kind == 126) {
						PushContext(Context.CollectionInitializer, la, t);
						goto case 152;
					} else {
						if (la.kind == 35) {
							PushContext(Context.CollectionInitializer, la, t);
							stateStack.Push(151);
							goto case 80;
						} else {
							if (la.kind == 233) {
								PushContext(Context.ObjectInitializer, la, t);
								goto case 148;
							} else {
								goto case 89;
							}
						}
					}
				} else {
					goto case 90;
				}
			}
			case 148: {
				if (la == null) { currentState = 148; break; }
				Expect(233, la); // "With"
				currentState = 149;
				break;
			}
			case 149: {
				stateStack.Push(150);
				goto case 94;
			}
			case 150: {
				PopContext();
				goto case 90;
			}
			case 151: {
				PopContext();
				goto case 90;
			}
			case 152: {
				if (la == null) { currentState = 152; break; }
				Expect(126, la); // "From"
				currentState = 153;
				break;
			}
			case 153: {
				if (la == null) { currentState = 153; break; }
				if (la.kind == 35) {
					stateStack.Push(154);
					goto case 80;
				} else {
					if (set[30].Get(la.kind)) {
						currentState = endOfStatementTerminatorAndBlock; /* leave this block */
							InformToken(t); /* process From again*/
							/* for processing current token (la): go to the position after processing End */
							goto switchlbl;

					} else {
						Error(la);
						goto case 154;
					}
				}
			}
			case 154: {
				PopContext();
				goto case 90;
			}
			case 155: {
				if (la == null) { currentState = 155; break; }
				currentState = 154;
				break;
			}
			case 156: {
				stateStack.Push(157);
				goto case 76;
			}
			case 157: {
				if (la == null) { currentState = 157; break; }
				Expect(144, la); // "Is"
				currentState = 158;
				break;
			}
			case 158: {
				PushContext(Context.Type, la, t);
				stateStack.Push(159);
				goto case 37;
			}
			case 159: {
				PopContext();
				goto case 79;
			}
			case 160: {
				if (la == null) { currentState = 160; break; }
				if (set[32].Get(la.kind)) {
					stateStack.Push(160);
					goto case 161;
				} else {
					goto case 79;
				}
			}
			case 161: {
				if (la == null) { currentState = 161; break; }
				if (la.kind == 37) {
					currentState = 166;
					break;
				} else {
					if (set[135].Get(la.kind)) {
						currentState = 162;
						break;
					} else {
						goto case 6;
					}
				}
			}
			case 162: {
				nextTokenIsStartOfImportsOrAccessExpression = true;
				goto case 163;
			}
			case 163: {
				if (la == null) { currentState = 163; break; }
				if (la.kind == 10) {
					currentState = 164;
					break;
				} else {
					goto case 164;
				}
			}
			case 164: {
				stateStack.Push(165);
				goto case 102;
			}
			case 165: {
				if (la == null) { currentState = 165; break; }
				if (la.kind == 11) {
					currentState = stateStack.Pop();
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 166: {
				PushContext(Context.Expression, la, t);
				nextTokenIsPotentialStartOfExpression = true;
				goto case 167;
			}
			case 167: {
				if (la == null) { currentState = 167; break; }
				if (la.kind == 169) {
					currentState = 169;
					break;
				} else {
					if (set[21].Get(la.kind)) {
						if (set[22].Get(la.kind)) {
							stateStack.Push(168);
							goto case 48;
						} else {
							goto case 168;
						}
					} else {
						Error(la);
						goto case 168;
					}
				}
			}
			case 168: {
				PopContext();
				goto case 45;
			}
			case 169: {
				PushContext(Context.Type, la, t);
				stateStack.Push(170);
				goto case 37;
			}
			case 170: {
				PopContext();
				goto case 171;
			}
			case 171: {
				if (la == null) { currentState = 171; break; }
				if (la.kind == 22) {
					currentState = 172;
					break;
				} else {
					goto case 168;
				}
			}
			case 172: {
				PushContext(Context.Type, la, t);
				stateStack.Push(173);
				goto case 37;
			}
			case 173: {
				PopContext();
				goto case 171;
			}
			case 174: {
				PushContext(Context.Expression, la, t);
				nextTokenIsPotentialStartOfExpression = true;
				goto case 175;
			}
			case 175: {
				if (la == null) { currentState = 175; break; }
				if (set[136].Get(la.kind)) {
					currentState = 176;
					break;
				} else {
					if (la.kind == 37) {
						currentState = 478;
						break;
					} else {
						if (set[137].Get(la.kind)) {
							currentState = 176;
							break;
						} else {
							if (set[133].Get(la.kind)) {
								currentState = 176;
								break;
							} else {
								if (set[135].Get(la.kind)) {
									currentState = 474;
									break;
								} else {
									if (la.kind == 129) {
										currentState = 471;
										break;
									} else {
										if (la.kind == 237) {
											currentState = 468;
											break;
										} else {
											if (set[83].Get(la.kind)) {
												stateStack.Push(176);
												nextTokenIsPotentialStartOfExpression = true;
												PushContext(Context.Xml, la, t);
												goto case 451;
											} else {
												if (la.kind == 127 || la.kind == 210) {
													stateStack.Push(176);
													goto case 253;
												} else {
													if (la.kind == 58 || la.kind == 126) {
														stateStack.Push(176);
														PushContext(Context.Query, la, t);
														goto case 191;
													} else {
														if (set[37].Get(la.kind)) {
															stateStack.Push(176);
															goto case 184;
														} else {
															if (la.kind == 135) {
																stateStack.Push(176);
																goto case 177;
															} else {
																Error(la);
																goto case 176;
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 176: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 177: {
				if (la == null) { currentState = 177; break; }
				Expect(135, la); // "If"
				currentState = 178;
				break;
			}
			case 178: {
				if (la == null) { currentState = 178; break; }
				Expect(37, la); // "("
				currentState = 179;
				break;
			}
			case 179: {
				stateStack.Push(180);
				goto case 56;
			}
			case 180: {
				if (la == null) { currentState = 180; break; }
				Expect(22, la); // ","
				currentState = 181;
				break;
			}
			case 181: {
				stateStack.Push(182);
				goto case 56;
			}
			case 182: {
				if (la == null) { currentState = 182; break; }
				if (la.kind == 22) {
					currentState = 183;
					break;
				} else {
					goto case 45;
				}
			}
			case 183: {
				stateStack.Push(45);
				goto case 56;
			}
			case 184: {
				if (la == null) { currentState = 184; break; }
				if (set[138].Get(la.kind)) {
					currentState = 190;
					break;
				} else {
					if (la.kind == 94 || la.kind == 106 || la.kind == 219) {
						currentState = 185;
						break;
					} else {
						goto case 6;
					}
				}
			}
			case 185: {
				if (la == null) { currentState = 185; break; }
				Expect(37, la); // "("
				currentState = 186;
				break;
			}
			case 186: {
				stateStack.Push(187);
				goto case 56;
			}
			case 187: {
				if (la == null) { currentState = 187; break; }
				Expect(22, la); // ","
				currentState = 188;
				break;
			}
			case 188: {
				PushContext(Context.Type, la, t);
				stateStack.Push(189);
				goto case 37;
			}
			case 189: {
				PopContext();
				goto case 45;
			}
			case 190: {
				if (la == null) { currentState = 190; break; }
				Expect(37, la); // "("
				currentState = 183;
				break;
			}
			case 191: {
				if (la == null) { currentState = 191; break; }
				if (la.kind == 126) {
					stateStack.Push(192);
					goto case 252;
				} else {
					if (la.kind == 58) {
						stateStack.Push(192);
						goto case 251;
					} else {
						Error(la);
						goto case 192;
					}
				}
			}
			case 192: {
				if (la == null) { currentState = 192; break; }
				if (set[38].Get(la.kind)) {
					stateStack.Push(192);
					goto case 193;
				} else {
					PopContext();
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 193: {
				if (la == null) { currentState = 193; break; }
				if (la.kind == 126) {
					currentState = 249;
					break;
				} else {
					if (la.kind == 58) {
						currentState = 245;
						break;
					} else {
						if (la.kind == 197) {
							currentState = 243;
							break;
						} else {
							if (la.kind == 107) {
								goto case 134;
							} else {
								if (la.kind == 230) {
									currentState = 56;
									break;
								} else {
									if (la.kind == 176) {
										currentState = 239;
										break;
									} else {
										if (la.kind == 203 || la.kind == 212) {
											currentState = 237;
											break;
										} else {
											if (la.kind == 148) {
												currentState = 235;
												break;
											} else {
												if (la.kind == 133) {
													currentState = 207;
													break;
												} else {
													if (la.kind == 146) {
														currentState = 194;
														break;
													} else {
														goto case 6;
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 194: {
				stateStack.Push(195);
				goto case 200;
			}
			case 195: {
				if (la == null) { currentState = 195; break; }
				Expect(171, la); // "On"
				currentState = 196;
				break;
			}
			case 196: {
				stateStack.Push(197);
				goto case 56;
			}
			case 197: {
				if (la == null) { currentState = 197; break; }
				Expect(116, la); // "Equals"
				currentState = 198;
				break;
			}
			case 198: {
				stateStack.Push(199);
				goto case 56;
			}
			case 199: {
				if (la == null) { currentState = 199; break; }
				if (la.kind == 22) {
					currentState = 196;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 200: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(201);
				goto case 206;
			}
			case 201: {
				PopContext();
				goto case 202;
			}
			case 202: {
				if (la == null) { currentState = 202; break; }
				if (la.kind == 63) {
					currentState = 204;
					break;
				} else {
					goto case 203;
				}
			}
			case 203: {
				if (la == null) { currentState = 203; break; }
				Expect(138, la); // "In"
				currentState = 56;
				break;
			}
			case 204: {
				PushContext(Context.Type, la, t);
				stateStack.Push(205);
				goto case 37;
			}
			case 205: {
				PopContext();
				goto case 203;
			}
			case 206: {
				if (la == null) { currentState = 206; break; }
				if (set[123].Get(la.kind)) {
					currentState = stateStack.Pop();
					break;
				} else {
					if (la.kind == 98) {
						goto case 136;
					} else {
						goto case 6;
					}
				}
			}
			case 207: {
				SetIdentifierExpected(la);
				nextTokenIsPotentialStartOfExpression = true;
				goto case 208;
			}
			case 208: {
				if (la == null) { currentState = 208; break; }
				if (la.kind == 146) {
					goto case 227;
				} else {
					if (set[40].Get(la.kind)) {
						if (la.kind == 70) {
							currentState = 210;
							break;
						} else {
							if (set[40].Get(la.kind)) {
								goto case 225;
							} else {
								Error(la);
								goto case 209;
							}
						}
					} else {
						goto case 6;
					}
				}
			}
			case 209: {
				if (la == null) { currentState = 209; break; }
				Expect(70, la); // "By"
				currentState = 210;
				break;
			}
			case 210: {
				stateStack.Push(211);
				goto case 214;
			}
			case 211: {
				if (la == null) { currentState = 211; break; }
				if (la.kind == 22) {
					currentState = 210;
					break;
				} else {
					Expect(143, la); // "Into"
					currentState = 212;
					break;
				}
			}
			case 212: {
				stateStack.Push(213);
				goto case 214;
			}
			case 213: {
				if (la == null) { currentState = 213; break; }
				if (la.kind == 22) {
					currentState = 212;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 214: {
				SetIdentifierExpected(la);
				nextTokenIsPotentialStartOfExpression = true;
				goto case 215;
			}
			case 215: {
				if (la == null) { currentState = 215; break; }
				if (set[6].Get(la.kind)) {
					PushContext(Context.Identifier, la, t);
					SetIdentifierExpected(la);
					stateStack.Push(218);
					goto case 206;
				} else {
					goto case 216;
				}
			}
			case 216: {
				stateStack.Push(217);
				goto case 56;
			}
			case 217: {
				if (!isAlreadyInExpr) PopContext(); isAlreadyInExpr = false;
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 218: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 219;
			}
			case 219: {
				if (la == null) { currentState = 219; break; }
				if (set[42].Get(la.kind)) {
					PopContext(); isAlreadyInExpr = true;
					goto case 220;
				} else {
					goto case 216;
				}
			}
			case 220: {
				if (la == null) { currentState = 220; break; }
				if (la.kind == 63) {
					currentState = 222;
					break;
				} else {
					if (la.kind == 20) {
						currentState = 216;
						break;
					} else {
						if (set[43].Get(la.kind)) {
							currentState = endOfStatementTerminatorAndBlock; /* leave this block */
								InformToken(t); /* process Identifier again*/
								/* for processing current token (la): go to the position after processing End */
								goto switchlbl;

						} else {
							Error(la);
							goto case 216;
						}
					}
				}
			}
			case 221: {
				if (la == null) { currentState = 221; break; }
				currentState = 216;
				break;
			}
			case 222: {
				PushContext(Context.Type, la, t);
				stateStack.Push(223);
				goto case 37;
			}
			case 223: {
				PopContext();
				goto case 224;
			}
			case 224: {
				if (la == null) { currentState = 224; break; }
				Expect(20, la); // "="
				currentState = 216;
				break;
			}
			case 225: {
				stateStack.Push(226);
				goto case 214;
			}
			case 226: {
				if (la == null) { currentState = 226; break; }
				if (la.kind == 22) {
					currentState = 225;
					break;
				} else {
					goto case 209;
				}
			}
			case 227: {
				stateStack.Push(228);
				goto case 234;
			}
			case 228: {
				if (la == null) { currentState = 228; break; }
				if (la.kind == 133 || la.kind == 146) {
					if (la.kind == 133) {
						currentState = 232;
						break;
					} else {
						if (la.kind == 146) {
							goto case 227;
						} else {
							Error(la);
							goto case 228;
						}
					}
				} else {
					goto case 229;
				}
			}
			case 229: {
				if (la == null) { currentState = 229; break; }
				Expect(143, la); // "Into"
				currentState = 230;
				break;
			}
			case 230: {
				stateStack.Push(231);
				goto case 214;
			}
			case 231: {
				if (la == null) { currentState = 231; break; }
				if (la.kind == 22) {
					currentState = 230;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 232: {
				stateStack.Push(233);
				goto case 234;
			}
			case 233: {
				stateStack.Push(228);
				goto case 229;
			}
			case 234: {
				if (la == null) { currentState = 234; break; }
				Expect(146, la); // "Join"
				currentState = 194;
				break;
			}
			case 235: {
				stateStack.Push(236);
				goto case 214;
			}
			case 236: {
				if (la == null) { currentState = 236; break; }
				if (la.kind == 22) {
					currentState = 235;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 237: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 238;
			}
			case 238: {
				if (la == null) { currentState = 238; break; }
				if (la.kind == 231) {
					currentState = 56;
					break;
				} else {
					goto case 56;
				}
			}
			case 239: {
				if (la == null) { currentState = 239; break; }
				Expect(70, la); // "By"
				currentState = 240;
				break;
			}
			case 240: {
				stateStack.Push(241);
				goto case 56;
			}
			case 241: {
				if (la == null) { currentState = 241; break; }
				if (la.kind == 64) {
					currentState = 242;
					break;
				} else {
					if (la.kind == 104) {
						currentState = 242;
						break;
					} else {
						Error(la);
						goto case 242;
					}
				}
			}
			case 242: {
				if (la == null) { currentState = 242; break; }
				if (la.kind == 22) {
					currentState = 240;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 243: {
				stateStack.Push(244);
				goto case 214;
			}
			case 244: {
				if (la == null) { currentState = 244; break; }
				if (la.kind == 22) {
					currentState = 243;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 245: {
				stateStack.Push(246);
				goto case 200;
			}
			case 246: {
				if (la == null) { currentState = 246; break; }
				if (set[38].Get(la.kind)) {
					stateStack.Push(246);
					goto case 193;
				} else {
					Expect(143, la); // "Into"
					currentState = 247;
					break;
				}
			}
			case 247: {
				stateStack.Push(248);
				goto case 214;
			}
			case 248: {
				if (la == null) { currentState = 248; break; }
				if (la.kind == 22) {
					currentState = 247;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 249: {
				stateStack.Push(250);
				goto case 200;
			}
			case 250: {
				if (la == null) { currentState = 250; break; }
				if (la.kind == 22) {
					currentState = 249;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 251: {
				if (la == null) { currentState = 251; break; }
				Expect(58, la); // "Aggregate"
				currentState = 245;
				break;
			}
			case 252: {
				if (la == null) { currentState = 252; break; }
				Expect(126, la); // "From"
				currentState = 249;
				break;
			}
			case 253: {
				if (la == null) { currentState = 253; break; }
				if (la.kind == 210) {
					currentState = 443;
					break;
				} else {
					if (la.kind == 127) {
						currentState = 254;
						break;
					} else {
						goto case 6;
					}
				}
			}
			case 254: {
				if (la == null) { currentState = 254; break; }
				Expect(37, la); // "("
				currentState = 255;
				break;
			}
			case 255: {
				SetIdentifierExpected(la);
				goto case 256;
			}
			case 256: {
				if (la == null) { currentState = 256; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(257);
					goto case 422;
				} else {
					goto case 257;
				}
			}
			case 257: {
				if (la == null) { currentState = 257; break; }
				Expect(38, la); // ")"
				currentState = 258;
				break;
			}
			case 258: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 259;
			}
			case 259: {
				if (la == null) { currentState = 259; break; }
				if (set[23].Get(la.kind)) {
					goto case 56;
				} else {
					if (la.kind == 1 || la.kind == 21 || la.kind == 63) {
						if (la.kind == 63) {
							currentState = 420;
							break;
						} else {
							goto case 260;
						}
					} else {
						goto case 6;
					}
				}
			}
			case 260: {
				stateStack.Push(261);
				goto case 263;
			}
			case 261: {
				if (la == null) { currentState = 261; break; }
				Expect(113, la); // "End"
				currentState = 262;
				break;
			}
			case 262: {
				if (la == null) { currentState = 262; break; }
				Expect(127, la); // "Function"
				currentState = stateStack.Pop();
				break;
			}
			case 263: {
				PushContext(Context.Body, la, t);
				goto case 264;
			}
			case 264: {
				stateStack.Push(265);
				goto case 23;
			}
			case 265: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 266;
			}
			case 266: {
				if (la == null) { currentState = 266; break; }
				if (set[139].Get(la.kind)) {
					if (set[70].Get(la.kind)) {
						if (set[51].Get(la.kind)) {
							stateStack.Push(264);
							goto case 271;
						} else {
							goto case 264;
						}
					} else {
						if (la.kind == 113) {
							currentState = 269;
							break;
						} else {
							goto case 268;
						}
					}
				} else {
					goto case 267;
				}
			}
			case 267: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 268: {
				Error(la);
				goto case 265;
			}
			case 269: {
				if (la == null) { currentState = 269; break; }
				if (la.kind == 1 || la.kind == 21) {
					goto case 264;
				} else {
					if (set[50].Get(la.kind)) {
						currentState = endOfStatementTerminatorAndBlock; /* leave this block */
						InformToken(t); /* process End again*/
						/* for processing current token (la): go to the position after processing End */
						goto switchlbl;

					} else {
						goto case 268;
					}
				}
			}
			case 270: {
				if (la == null) { currentState = 270; break; }
				currentState = 265;
				break;
			}
			case 271: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 272;
			}
			case 272: {
				if (la == null) { currentState = 272; break; }
				if (la.kind == 88 || la.kind == 105 || la.kind == 204) {
					currentState = 400;
					break;
				} else {
					if (la.kind == 211 || la.kind == 233) {
						currentState = 396;
						break;
					} else {
						if (la.kind == 56 || la.kind == 193) {
							currentState = 394;
							break;
						} else {
							if (la.kind == 189) {
								currentState = 392;
								break;
							} else {
								if (la.kind == 135) {
									currentState = 374;
									break;
								} else {
									if (la.kind == 197) {
										currentState = 358;
										break;
									} else {
										if (la.kind == 231) {
											currentState = 354;
											break;
										} else {
											if (la.kind == 108) {
												currentState = 348;
												break;
											} else {
												if (la.kind == 124) {
													currentState = 321;
													break;
												} else {
													if (la.kind == 118 || la.kind == 171 || la.kind == 194) {
														if (la.kind == 118 || la.kind == 171) {
															if (la.kind == 171) {
																currentState = 317;
																break;
															} else {
																goto case 317;
															}
														} else {
															if (la.kind == 194) {
																currentState = 315;
																break;
															} else {
																goto case 6;
															}
														}
													} else {
														if (la.kind == 215) {
															currentState = 313;
															break;
														} else {
															if (la.kind == 218) {
																currentState = 300;
																break;
															} else {
																if (set[140].Get(la.kind)) {
																	if (la.kind == 132) {
																		currentState = 297;
																		break;
																	} else {
																		if (la.kind == 120) {
																			currentState = 296;
																			break;
																		} else {
																			if (la.kind == 89) {
																				currentState = 295;
																				break;
																			} else {
																				if (la.kind == 206) {
																					goto case 114;
																				} else {
																					if (la.kind == 195) {
																						currentState = 292;
																						break;
																					} else {
																						goto case 6;
																					}
																				}
																			}
																		}
																	}
																} else {
																	if (la.kind == 191) {
																		currentState = 290;
																		break;
																	} else {
																		if (la.kind == 117) {
																			currentState = 288;
																			break;
																		} else {
																			if (la.kind == 226) {
																				currentState = 273;
																				break;
																			} else {
																				if (set[141].Get(la.kind)) {
																					if (la.kind == 73) {
																						currentState = 56;
																						break;
																					} else {
																						goto case 56;
																					}
																				} else {
																					goto case 6;
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 273: {
				stateStack.Push(274);
				SetIdentifierExpected(la);
				nextTokenIsPotentialStartOfExpression = true;
				goto case 277;
			}
			case 274: {
				if (la == null) { currentState = 274; break; }
				if (la.kind == 22) {
					currentState = 273;
					break;
				} else {
					stateStack.Push(275);
					goto case 263;
				}
			}
			case 275: {
				if (la == null) { currentState = 275; break; }
				Expect(113, la); // "End"
				currentState = 276;
				break;
			}
			case 276: {
				if (la == null) { currentState = 276; break; }
				Expect(226, la); // "Using"
				currentState = stateStack.Pop();
				break;
			}
			case 277: {
				if (la == null) { currentState = 277; break; }
				if (set[6].Get(la.kind)) {
					PushContext(Context.Identifier, la, t);
					SetIdentifierExpected(la);
					stateStack.Push(280);
					goto case 206;
				} else {
					goto case 278;
				}
			}
			case 278: {
				stateStack.Push(279);
				goto case 56;
			}
			case 279: {
				if (!isAlreadyInExpr) PopContext(); isAlreadyInExpr = false;
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 280: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 281;
			}
			case 281: {
				if (la == null) { currentState = 281; break; }
				if (set[42].Get(la.kind)) {
					PopContext(); isAlreadyInExpr = true;
					goto case 282;
				} else {
					goto case 278;
				}
			}
			case 282: {
				if (la == null) { currentState = 282; break; }
				if (la.kind == 63) {
					currentState = 284;
					break;
				} else {
					if (la.kind == 20) {
						currentState = 278;
						break;
					} else {
						if (set[43].Get(la.kind)) {
							currentState = endOfStatementTerminatorAndBlock; /* leave this block */
								InformToken(t); /* process Identifier again*/
								/* for processing current token (la): go to the position after processing End */
								goto switchlbl;

						} else {
							Error(la);
							goto case 278;
						}
					}
				}
			}
			case 283: {
				if (la == null) { currentState = 283; break; }
				currentState = 278;
				break;
			}
			case 284: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 285;
			}
			case 285: {
				if (la == null) { currentState = 285; break; }
				if (set[16].Get(la.kind)) {
					PushContext(Context.Type, la, t);
					stateStack.Push(286);
					goto case 37;
				} else {
					goto case 278;
				}
			}
			case 286: {
				PopContext();
				goto case 287;
			}
			case 287: {
				if (la == null) { currentState = 287; break; }
				Expect(20, la); // "="
				currentState = 278;
				break;
			}
			case 288: {
				stateStack.Push(289);
				goto case 56;
			}
			case 289: {
				if (la == null) { currentState = 289; break; }
				if (la.kind == 22) {
					currentState = 288;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 290: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 291;
			}
			case 291: {
				if (la == null) { currentState = 291; break; }
				if (la.kind == 184) {
					currentState = 56;
					break;
				} else {
					goto case 56;
				}
			}
			case 292: {
				PushContext(Context.Expression, la, t);
				nextTokenIsPotentialStartOfExpression = true;
				goto case 293;
			}
			case 293: {
				if (la == null) { currentState = 293; break; }
				if (set[23].Get(la.kind)) {
					stateStack.Push(294);
					goto case 56;
				} else {
					goto case 294;
				}
			}
			case 294: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 295: {
				if (la == null) { currentState = 295; break; }
				if (la.kind == 108) {
					goto case 133;
				} else {
					if (la.kind == 124) {
						goto case 130;
					} else {
						if (la.kind == 231) {
							goto case 104;
						} else {
							goto case 6;
						}
					}
				}
			}
			case 296: {
				if (la == null) { currentState = 296; break; }
				if (la.kind == 108) {
					goto case 133;
				} else {
					if (la.kind == 124) {
						goto case 130;
					} else {
						if (la.kind == 231) {
							goto case 104;
						} else {
							if (la.kind == 197) {
								goto case 116;
							} else {
								if (la.kind == 210) {
									goto case 112;
								} else {
									if (la.kind == 127) {
										goto case 128;
									} else {
										if (la.kind == 186) {
											goto case 117;
										} else {
											if (la.kind == 218) {
												goto case 108;
											} else {
												goto case 6;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 297: {
				if (la == null) { currentState = 297; break; }
				if (set[6].Get(la.kind)) {
					goto case 299;
				} else {
					if (la.kind == 5) {
						goto case 298;
					} else {
						goto case 6;
					}
				}
			}
			case 298: {
				if (la == null) { currentState = 298; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 299: {
				if (la == null) { currentState = 299; break; }
				currentState = stateStack.Pop();
				break;
			}
			case 300: {
				stateStack.Push(301);
				goto case 263;
			}
			case 301: {
				if (la == null) { currentState = 301; break; }
				if (la.kind == 75) {
					currentState = 305;
					break;
				} else {
					if (la.kind == 123) {
						currentState = 304;
						break;
					} else {
						goto case 302;
					}
				}
			}
			case 302: {
				if (la == null) { currentState = 302; break; }
				Expect(113, la); // "End"
				currentState = 303;
				break;
			}
			case 303: {
				if (la == null) { currentState = 303; break; }
				Expect(218, la); // "Try"
				currentState = stateStack.Pop();
				break;
			}
			case 304: {
				stateStack.Push(302);
				goto case 263;
			}
			case 305: {
				SetIdentifierExpected(la);
				goto case 306;
			}
			case 306: {
				if (la == null) { currentState = 306; break; }
				if (set[6].Get(la.kind)) {
					PushContext(Context.Identifier, la, t);
					SetIdentifierExpected(la);
					stateStack.Push(309);
					goto case 206;
				} else {
					goto case 307;
				}
			}
			case 307: {
				if (la == null) { currentState = 307; break; }
				if (la.kind == 229) {
					currentState = 308;
					break;
				} else {
					goto case 300;
				}
			}
			case 308: {
				stateStack.Push(300);
				goto case 56;
			}
			case 309: {
				PopContext();
				goto case 310;
			}
			case 310: {
				if (la == null) { currentState = 310; break; }
				if (la.kind == 63) {
					currentState = 311;
					break;
				} else {
					goto case 307;
				}
			}
			case 311: {
				PushContext(Context.Type, la, t);
				stateStack.Push(312);
				goto case 37;
			}
			case 312: {
				PopContext();
				goto case 307;
			}
			case 313: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 314;
			}
			case 314: {
				if (la == null) { currentState = 314; break; }
				if (set[23].Get(la.kind)) {
					goto case 56;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 315: {
				if (la == null) { currentState = 315; break; }
				if (la.kind == 163) {
					goto case 121;
				} else {
					goto case 316;
				}
			}
			case 316: {
				if (la == null) { currentState = 316; break; }
				if (la.kind == 5) {
					goto case 298;
				} else {
					if (set[6].Get(la.kind)) {
						goto case 299;
					} else {
						goto case 6;
					}
				}
			}
			case 317: {
				if (la == null) { currentState = 317; break; }
				Expect(118, la); // "Error"
				currentState = 318;
				break;
			}
			case 318: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 319;
			}
			case 319: {
				if (la == null) { currentState = 319; break; }
				if (set[23].Get(la.kind)) {
					goto case 56;
				} else {
					if (la.kind == 132) {
						currentState = 316;
						break;
					} else {
						if (la.kind == 194) {
							currentState = 320;
							break;
						} else {
							goto case 6;
						}
					}
				}
			}
			case 320: {
				if (la == null) { currentState = 320; break; }
				Expect(163, la); // "Next"
				currentState = stateStack.Pop();
				break;
			}
			case 321: {
				nextTokenIsPotentialStartOfExpression = true;
				SetIdentifierExpected(la);
				goto case 322;
			}
			case 322: {
				if (la == null) { currentState = 322; break; }
				if (set[35].Get(la.kind)) {
					stateStack.Push(338);
					goto case 332;
				} else {
					if (la.kind == 110) {
						currentState = 323;
						break;
					} else {
						goto case 6;
					}
				}
			}
			case 323: {
				stateStack.Push(324);
				goto case 332;
			}
			case 324: {
				if (la == null) { currentState = 324; break; }
				Expect(138, la); // "In"
				currentState = 325;
				break;
			}
			case 325: {
				stateStack.Push(326);
				goto case 56;
			}
			case 326: {
				stateStack.Push(327);
				goto case 263;
			}
			case 327: {
				if (la == null) { currentState = 327; break; }
				Expect(163, la); // "Next"
				currentState = 328;
				break;
			}
			case 328: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 329;
			}
			case 329: {
				if (la == null) { currentState = 329; break; }
				if (set[23].Get(la.kind)) {
					goto case 330;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 330: {
				stateStack.Push(331);
				goto case 56;
			}
			case 331: {
				if (la == null) { currentState = 331; break; }
				if (la.kind == 22) {
					currentState = 330;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 332: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(333);
				goto case 174;
			}
			case 333: {
				PopContext();
				goto case 334;
			}
			case 334: {
				if (la == null) { currentState = 334; break; }
				if (la.kind == 33) {
					currentState = 335;
					break;
				} else {
					goto case 335;
				}
			}
			case 335: {
				if (la == null) { currentState = 335; break; }
				if (set[32].Get(la.kind)) {
					stateStack.Push(335);
					goto case 161;
				} else {
					if (la.kind == 63) {
						currentState = 336;
						break;
					} else {
						currentState = stateStack.Pop();
						goto switchlbl;
					}
				}
			}
			case 336: {
				PushContext(Context.Type, la, t);
				stateStack.Push(337);
				goto case 37;
			}
			case 337: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 338: {
				if (la == null) { currentState = 338; break; }
				Expect(20, la); // "="
				currentState = 339;
				break;
			}
			case 339: {
				stateStack.Push(340);
				goto case 56;
			}
			case 340: {
				if (la == null) { currentState = 340; break; }
				if (la.kind == 205) {
					currentState = 347;
					break;
				} else {
					goto case 341;
				}
			}
			case 341: {
				stateStack.Push(342);
				goto case 263;
			}
			case 342: {
				if (la == null) { currentState = 342; break; }
				Expect(163, la); // "Next"
				currentState = 343;
				break;
			}
			case 343: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 344;
			}
			case 344: {
				if (la == null) { currentState = 344; break; }
				if (set[23].Get(la.kind)) {
					goto case 345;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 345: {
				stateStack.Push(346);
				goto case 56;
			}
			case 346: {
				if (la == null) { currentState = 346; break; }
				if (la.kind == 22) {
					currentState = 345;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 347: {
				stateStack.Push(341);
				goto case 56;
			}
			case 348: {
				if (la == null) { currentState = 348; break; }
				if (la.kind == 224 || la.kind == 231) {
					currentState = 351;
					break;
				} else {
					if (la.kind == 1 || la.kind == 21) {
						stateStack.Push(349);
						goto case 263;
					} else {
						goto case 6;
					}
				}
			}
			case 349: {
				if (la == null) { currentState = 349; break; }
				Expect(152, la); // "Loop"
				currentState = 350;
				break;
			}
			case 350: {
				if (la == null) { currentState = 350; break; }
				if (la.kind == 224 || la.kind == 231) {
					currentState = 56;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 351: {
				stateStack.Push(352);
				goto case 56;
			}
			case 352: {
				stateStack.Push(353);
				goto case 263;
			}
			case 353: {
				if (la == null) { currentState = 353; break; }
				Expect(152, la); // "Loop"
				currentState = stateStack.Pop();
				break;
			}
			case 354: {
				stateStack.Push(355);
				goto case 56;
			}
			case 355: {
				stateStack.Push(356);
				goto case 263;
			}
			case 356: {
				if (la == null) { currentState = 356; break; }
				Expect(113, la); // "End"
				currentState = 357;
				break;
			}
			case 357: {
				if (la == null) { currentState = 357; break; }
				Expect(231, la); // "While"
				currentState = stateStack.Pop();
				break;
			}
			case 358: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 359;
			}
			case 359: {
				if (la == null) { currentState = 359; break; }
				if (la.kind == 74) {
					currentState = 360;
					break;
				} else {
					goto case 360;
				}
			}
			case 360: {
				stateStack.Push(361);
				goto case 56;
			}
			case 361: {
				stateStack.Push(362);
				goto case 23;
			}
			case 362: {
				if (la == null) { currentState = 362; break; }
				if (la.kind == 74) {
					currentState = 364;
					break;
				} else {
					Expect(113, la); // "End"
					currentState = 363;
					break;
				}
			}
			case 363: {
				if (la == null) { currentState = 363; break; }
				Expect(197, la); // "Select"
				currentState = stateStack.Pop();
				break;
			}
			case 364: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 365;
			}
			case 365: {
				if (la == null) { currentState = 365; break; }
				if (la.kind == 111) {
					currentState = 366;
					break;
				} else {
					if (set[68].Get(la.kind)) {
						goto case 367;
					} else {
						Error(la);
						goto case 366;
					}
				}
			}
			case 366: {
				stateStack.Push(362);
				goto case 263;
			}
			case 367: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 368;
			}
			case 368: {
				if (la == null) { currentState = 368; break; }
				if (set[142].Get(la.kind)) {
					if (la.kind == 144) {
						currentState = 370;
						break;
					} else {
						goto case 370;
					}
				} else {
					if (set[23].Get(la.kind)) {
						stateStack.Push(369);
						goto case 56;
					} else {
						Error(la);
						goto case 369;
					}
				}
			}
			case 369: {
				if (la == null) { currentState = 369; break; }
				if (la.kind == 22) {
					currentState = 367;
					break;
				} else {
					goto case 366;
				}
			}
			case 370: {
				stateStack.Push(371);
				goto case 372;
			}
			case 371: {
				stateStack.Push(369);
				goto case 76;
			}
			case 372: {
				if (la == null) { currentState = 372; break; }
				if (la.kind == 20) {
					goto case 74;
				} else {
					if (la.kind == 41) {
						goto case 73;
					} else {
						if (la.kind == 40) {
							goto case 72;
						} else {
							if (la.kind == 39) {
								currentState = 373;
								break;
							} else {
								if (la.kind == 42) {
									goto case 69;
								} else {
									if (la.kind == 43) {
										goto case 70;
									} else {
										goto case 6;
									}
								}
							}
						}
					}
				}
			}
			case 373: {
				wasNormalAttribute = false;
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 374: {
				stateStack.Push(375);
				goto case 56;
			}
			case 375: {
				if (la == null) { currentState = 375; break; }
				if (la.kind == 214) {
					currentState = 383;
					break;
				} else {
					goto case 376;
				}
			}
			case 376: {
				if (la == null) { currentState = 376; break; }
				if (la.kind == 1 || la.kind == 21) {
					goto case 377;
				} else {
					goto case 6;
				}
			}
			case 377: {
				stateStack.Push(378);
				goto case 263;
			}
			case 378: {
				if (la == null) { currentState = 378; break; }
				if (la.kind == 111 || la.kind == 112) {
					if (la.kind == 111) {
						currentState = 382;
						break;
					} else {
						if (la.kind == 112) {
							currentState = 380;
							break;
						} else {
							Error(la);
							goto case 377;
						}
					}
				} else {
					Expect(113, la); // "End"
					currentState = 379;
					break;
				}
			}
			case 379: {
				if (la == null) { currentState = 379; break; }
				Expect(135, la); // "If"
				currentState = stateStack.Pop();
				break;
			}
			case 380: {
				stateStack.Push(381);
				goto case 56;
			}
			case 381: {
				if (la == null) { currentState = 381; break; }
				if (la.kind == 214) {
					currentState = 377;
					break;
				} else {
					goto case 377;
				}
			}
			case 382: {
				if (la == null) { currentState = 382; break; }
				if (la.kind == 135) {
					currentState = 380;
					break;
				} else {
					goto case 377;
				}
			}
			case 383: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 384;
			}
			case 384: {
				if (la == null) { currentState = 384; break; }
				if (set[51].Get(la.kind)) {
					goto case 385;
				} else {
					goto case 376;
				}
			}
			case 385: {
				stateStack.Push(386);
				goto case 271;
			}
			case 386: {
				if (la == null) { currentState = 386; break; }
				if (la.kind == 21) {
					currentState = 390;
					break;
				} else {
					if (la.kind == 111) {
						currentState = 387;
						break;
					} else {
						currentState = stateStack.Pop();
						goto switchlbl;
					}
				}
			}
			case 387: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 388;
			}
			case 388: {
				if (la == null) { currentState = 388; break; }
				if (set[51].Get(la.kind)) {
					stateStack.Push(389);
					goto case 271;
				} else {
					goto case 389;
				}
			}
			case 389: {
				if (la == null) { currentState = 389; break; }
				if (la.kind == 21) {
					currentState = 387;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 390: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 391;
			}
			case 391: {
				if (la == null) { currentState = 391; break; }
				if (set[51].Get(la.kind)) {
					goto case 385;
				} else {
					goto case 386;
				}
			}
			case 392: {
				stateStack.Push(393);
				goto case 102;
			}
			case 393: {
				if (la == null) { currentState = 393; break; }
				if (la.kind == 37) {
					currentState = 46;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 394: {
				stateStack.Push(395);
				goto case 56;
			}
			case 395: {
				if (la == null) { currentState = 395; break; }
				Expect(22, la); // ","
				currentState = 56;
				break;
			}
			case 396: {
				stateStack.Push(397);
				goto case 56;
			}
			case 397: {
				stateStack.Push(398);
				goto case 263;
			}
			case 398: {
				if (la == null) { currentState = 398; break; }
				Expect(113, la); // "End"
				currentState = 399;
				break;
			}
			case 399: {
				if (la == null) { currentState = 399; break; }
				if (la.kind == 233) {
					goto case 103;
				} else {
					if (la.kind == 211) {
						goto case 111;
					} else {
						goto case 6;
					}
				}
			}
			case 400: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(401);
				goto case 206;
			}
			case 401: {
				PopContext();
				goto case 402;
			}
			case 402: {
				if (la == null) { currentState = 402; break; }
				if (la.kind == 33) {
					currentState = 403;
					break;
				} else {
					goto case 403;
				}
			}
			case 403: {
				if (la == null) { currentState = 403; break; }
				if (la.kind == 37) {
					currentState = 417;
					break;
				} else {
					goto case 404;
				}
			}
			case 404: {
				if (la == null) { currentState = 404; break; }
				if (la.kind == 22) {
					currentState = 410;
					break;
				} else {
					if (la.kind == 63) {
						currentState = 407;
						break;
					} else {
						goto case 405;
					}
				}
			}
			case 405: {
				if (la == null) { currentState = 405; break; }
				if (la.kind == 20) {
					goto case 406;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 406: {
				if (la == null) { currentState = 406; break; }
				currentState = 56;
				break;
			}
			case 407: {
				PushContext(Context.Type, la, t);
				goto case 408;
			}
			case 408: {
				if (la == null) { currentState = 408; break; }
				if (la.kind == 162) {
					stateStack.Push(409);
					goto case 86;
				} else {
					if (set[16].Get(la.kind)) {
						stateStack.Push(409);
						goto case 37;
					} else {
						Error(la);
						goto case 409;
					}
				}
			}
			case 409: {
				PopContext();
				goto case 405;
			}
			case 410: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(411);
				goto case 206;
			}
			case 411: {
				PopContext();
				goto case 412;
			}
			case 412: {
				if (la == null) { currentState = 412; break; }
				if (la.kind == 33) {
					currentState = 413;
					break;
				} else {
					goto case 413;
				}
			}
			case 413: {
				if (la == null) { currentState = 413; break; }
				if (la.kind == 37) {
					currentState = 414;
					break;
				} else {
					goto case 404;
				}
			}
			case 414: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 415;
			}
			case 415: {
				if (la == null) { currentState = 415; break; }
				if (set[23].Get(la.kind)) {
					stateStack.Push(416);
					goto case 56;
				} else {
					goto case 416;
				}
			}
			case 416: {
				if (la == null) { currentState = 416; break; }
				if (la.kind == 22) {
					currentState = 414;
					break;
				} else {
					Expect(38, la); // ")"
					currentState = 413;
					break;
				}
			}
			case 417: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 418;
			}
			case 418: {
				if (la == null) { currentState = 418; break; }
				if (set[23].Get(la.kind)) {
					stateStack.Push(419);
					goto case 56;
				} else {
					goto case 419;
				}
			}
			case 419: {
				if (la == null) { currentState = 419; break; }
				if (la.kind == 22) {
					currentState = 417;
					break;
				} else {
					Expect(38, la); // ")"
					currentState = 403;
					break;
				}
			}
			case 420: {
				PushContext(Context.Type, la, t);
				stateStack.Push(421);
				goto case 37;
			}
			case 421: {
				PopContext();
				goto case 260;
			}
			case 422: {
				stateStack.Push(423);
				PushContext(Context.Parameter, la, t);
				goto case 424;
			}
			case 423: {
				if (la == null) { currentState = 423; break; }
				if (la.kind == 22) {
					currentState = 422;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 424: {
				SetIdentifierExpected(la);
				goto case 425;
			}
			case 425: {
				if (la == null) { currentState = 425; break; }
				if (la.kind == 40) {
					stateStack.Push(424);
					goto case 435;
				} else {
					goto case 426;
				}
			}
			case 426: {
				SetIdentifierExpected(la);
				goto case 427;
			}
			case 427: {
				if (la == null) { currentState = 427; break; }
				if (set[143].Get(la.kind)) {
					currentState = 426;
					break;
				} else {
					PushContext(Context.Identifier, la, t);
					SetIdentifierExpected(la);
					stateStack.Push(428);
					goto case 206;
				}
			}
			case 428: {
				PopContext();
				goto case 429;
			}
			case 429: {
				if (la == null) { currentState = 429; break; }
				if (la.kind == 63) {
					currentState = 433;
					break;
				} else {
					goto case 430;
				}
			}
			case 430: {
				if (la == null) { currentState = 430; break; }
				if (la.kind == 20) {
					currentState = 432;
					break;
				} else {
					goto case 431;
				}
			}
			case 431: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 432: {
				stateStack.Push(431);
				goto case 56;
			}
			case 433: {
				PushContext(Context.Type, la, t);
				stateStack.Push(434);
				goto case 37;
			}
			case 434: {
				PopContext();
				goto case 430;
			}
			case 435: {
				if (la == null) { currentState = 435; break; }
				Expect(40, la); // "<"
				currentState = 436;
				break;
			}
			case 436: {
				wasNormalAttribute = true; PushContext(Context.Attribute, la, t);
				goto case 437;
			}
			case 437: {
				if (la == null) { currentState = 437; break; }
				if (la.kind == 65 || la.kind == 155) {
					currentState = 441;
					break;
				} else {
					goto case 438;
				}
			}
			case 438: {
				if (la == null) { currentState = 438; break; }
				if (set[144].Get(la.kind)) {
					currentState = 438;
					break;
				} else {
					Expect(39, la); // ">"
					currentState = 439;
					break;
				}
			}
			case 439: {
				PopContext();
				goto case 440;
			}
			case 440: {
				if (la == null) { currentState = 440; break; }
				if (la.kind == 1) {
					goto case 25;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 441: {
				if (la == null) { currentState = 441; break; }
				Expect(21, la); // ":"
				currentState = 442;
				break;
			}
			case 442: {
				wasNormalAttribute = false;
				goto case 438;
			}
			case 443: {
				if (la == null) { currentState = 443; break; }
				Expect(37, la); // "("
				currentState = 444;
				break;
			}
			case 444: {
				SetIdentifierExpected(la);
				goto case 445;
			}
			case 445: {
				if (la == null) { currentState = 445; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(446);
					goto case 422;
				} else {
					goto case 446;
				}
			}
			case 446: {
				if (la == null) { currentState = 446; break; }
				Expect(38, la); // ")"
				currentState = 447;
				break;
			}
			case 447: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 448;
			}
			case 448: {
				if (la == null) { currentState = 448; break; }
				if (set[51].Get(la.kind)) {
					goto case 271;
				} else {
					if (la.kind == 1 || la.kind == 21) {
						stateStack.Push(449);
						goto case 263;
					} else {
						goto case 6;
					}
				}
			}
			case 449: {
				if (la == null) { currentState = 449; break; }
				Expect(113, la); // "End"
				currentState = 450;
				break;
			}
			case 450: {
				if (la == null) { currentState = 450; break; }
				Expect(210, la); // "Sub"
				currentState = stateStack.Pop();
				break;
			}
			case 451: {
				if (la == null) { currentState = 451; break; }
				if (la.kind == 17 || la.kind == 18 || la.kind == 19) {
					currentState = 464;
					break;
				} else {
					if (la.kind == 10) {
						stateStack.Push(453);
						goto case 455;
					} else {
						Error(la);
						goto case 452;
					}
				}
			}
			case 452: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 453: {
				if (la == null) { currentState = 453; break; }
				if (la.kind == 17) {
					currentState = 454;
					break;
				} else {
					goto case 452;
				}
			}
			case 454: {
				if (la == null) { currentState = 454; break; }
				if (la.kind == 16) {
					currentState = 453;
					break;
				} else {
					goto case 453;
				}
			}
			case 455: {
				PushContext(Context.Xml, la, t);
				goto case 456;
			}
			case 456: {
				if (la == null) { currentState = 456; break; }
				Expect(10, la); // XmlOpenTag
				currentState = 457;
				break;
			}
			case 457: {
				if (la == null) { currentState = 457; break; }
				if (set[145].Get(la.kind)) {
					if (set[146].Get(la.kind)) {
						currentState = 457;
						break;
					} else {
						if (la.kind == 12) {
							stateStack.Push(457);
							goto case 461;
						} else {
							Error(la);
							goto case 457;
						}
					}
				} else {
					if (la.kind == 14) {
						currentState = 458;
						break;
					} else {
						if (la.kind == 11) {
							currentState = 459;
							break;
						} else {
							Error(la);
							goto case 458;
						}
					}
				}
			}
			case 458: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 459: {
				if (la == null) { currentState = 459; break; }
				if (set[147].Get(la.kind)) {
					if (set[148].Get(la.kind)) {
						currentState = 459;
						break;
					} else {
						if (la.kind == 12) {
							stateStack.Push(459);
							goto case 461;
						} else {
							if (la.kind == 10) {
								stateStack.Push(459);
								goto case 455;
							} else {
								Error(la);
								goto case 459;
							}
						}
					}
				} else {
					Expect(15, la); // XmlOpenEndTag
					currentState = 460;
					break;
				}
			}
			case 460: {
				if (la == null) { currentState = 460; break; }
				if (set[149].Get(la.kind)) {
					if (set[150].Get(la.kind)) {
						currentState = 460;
						break;
					} else {
						if (la.kind == 12) {
							stateStack.Push(460);
							goto case 461;
						} else {
							Error(la);
							goto case 460;
						}
					}
				} else {
					Expect(11, la); // XmlCloseTag
					currentState = 458;
					break;
				}
			}
			case 461: {
				if (la == null) { currentState = 461; break; }
				Expect(12, la); // XmlStartInlineVB
				currentState = 462;
				break;
			}
			case 462: {
				stateStack.Push(463);
				goto case 56;
			}
			case 463: {
				if (la == null) { currentState = 463; break; }
				Expect(13, la); // XmlEndInlineVB
				currentState = stateStack.Pop();
				break;
			}
			case 464: {
				if (la == null) { currentState = 464; break; }
				if (la.kind == 16) {
					currentState = 465;
					break;
				} else {
					goto case 465;
				}
			}
			case 465: {
				if (la == null) { currentState = 465; break; }
				if (la.kind == 17 || la.kind == 19) {
					currentState = 464;
					break;
				} else {
					if (la.kind == 10) {
						stateStack.Push(466);
						goto case 455;
					} else {
						goto case 452;
					}
				}
			}
			case 466: {
				if (la == null) { currentState = 466; break; }
				if (la.kind == 17) {
					currentState = 467;
					break;
				} else {
					goto case 452;
				}
			}
			case 467: {
				if (la == null) { currentState = 467; break; }
				if (la.kind == 16) {
					currentState = 466;
					break;
				} else {
					goto case 466;
				}
			}
			case 468: {
				if (la == null) { currentState = 468; break; }
				Expect(37, la); // "("
				currentState = 469;
				break;
			}
			case 469: {
				readXmlIdentifier = true;
				stateStack.Push(470);
				goto case 206;
			}
			case 470: {
				if (la == null) { currentState = 470; break; }
				Expect(38, la); // ")"
				currentState = 176;
				break;
			}
			case 471: {
				if (la == null) { currentState = 471; break; }
				Expect(37, la); // "("
				currentState = 472;
				break;
			}
			case 472: {
				PushContext(Context.Type, la, t);
				stateStack.Push(473);
				goto case 37;
			}
			case 473: {
				PopContext();
				goto case 470;
			}
			case 474: {
				nextTokenIsStartOfImportsOrAccessExpression = true; wasQualifierTokenAtStart = true;
				goto case 475;
			}
			case 475: {
				if (la == null) { currentState = 475; break; }
				if (la.kind == 10) {
					currentState = 476;
					break;
				} else {
					goto case 476;
				}
			}
			case 476: {
				stateStack.Push(477);
				goto case 102;
			}
			case 477: {
				if (la == null) { currentState = 477; break; }
				if (la.kind == 11) {
					currentState = 176;
					break;
				} else {
					goto case 176;
				}
			}
			case 478: {
				activeArgument = 0;
				goto case 479;
			}
			case 479: {
				stateStack.Push(480);
				goto case 56;
			}
			case 480: {
				if (la == null) { currentState = 480; break; }
				if (la.kind == 22) {
					currentState = 481;
					break;
				} else {
					goto case 470;
				}
			}
			case 481: {
				activeArgument++;
				goto case 479;
			}
			case 482: {
				stateStack.Push(483);
				goto case 56;
			}
			case 483: {
				if (la == null) { currentState = 483; break; }
				if (la.kind == 22) {
					currentState = 484;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 484: {
				activeArgument++;
				nextTokenIsPotentialStartOfExpression = true;
				goto case 485;
			}
			case 485: {
				if (la == null) { currentState = 485; break; }
				if (set[23].Get(la.kind)) {
					goto case 482;
				} else {
					goto case 483;
				}
			}
			case 486: {
				if (la == null) { currentState = 486; break; }
				if (set[16].Get(la.kind)) {
					stateStack.Push(487);
					goto case 37;
				} else {
					goto case 487;
				}
			}
			case 487: {
				if (la == null) { currentState = 487; break; }
				if (la.kind == 22) {
					currentState = 486;
					break;
				} else {
					goto case 45;
				}
			}
			case 488: {
				SetIdentifierExpected(la);
				goto case 489;
			}
			case 489: {
				if (la == null) { currentState = 489; break; }
				if (set[151].Get(la.kind)) {
					if (la.kind == 169) {
						currentState = 491;
						break;
					} else {
						if (set[78].Get(la.kind)) {
							stateStack.Push(490);
							goto case 422;
						} else {
							Error(la);
							goto case 490;
						}
					}
				} else {
					goto case 490;
				}
			}
			case 490: {
				if (la == null) { currentState = 490; break; }
				Expect(38, la); // ")"
				currentState = 34;
				break;
			}
			case 491: {
				stateStack.Push(490);
				goto case 492;
			}
			case 492: {
				SetIdentifierExpected(la);
				goto case 493;
			}
			case 493: {
				if (la == null) { currentState = 493; break; }
				if (la.kind == 138 || la.kind == 178) {
					currentState = 494;
					break;
				} else {
					goto case 494;
				}
			}
			case 494: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(495);
				goto case 509;
			}
			case 495: {
				PopContext();
				goto case 496;
			}
			case 496: {
				if (la == null) { currentState = 496; break; }
				if (la.kind == 63) {
					currentState = 510;
					break;
				} else {
					goto case 497;
				}
			}
			case 497: {
				if (la == null) { currentState = 497; break; }
				if (la.kind == 22) {
					currentState = 498;
					break;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 498: {
				SetIdentifierExpected(la);
				goto case 499;
			}
			case 499: {
				if (la == null) { currentState = 499; break; }
				if (la.kind == 138 || la.kind == 178) {
					currentState = 500;
					break;
				} else {
					goto case 500;
				}
			}
			case 500: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(501);
				goto case 509;
			}
			case 501: {
				PopContext();
				goto case 502;
			}
			case 502: {
				if (la == null) { currentState = 502; break; }
				if (la.kind == 63) {
					currentState = 503;
					break;
				} else {
					goto case 497;
				}
			}
			case 503: {
				PushContext(Context.Type, la, t);
				stateStack.Push(504);
				goto case 505;
			}
			case 504: {
				PopContext();
				goto case 497;
			}
			case 505: {
				if (la == null) { currentState = 505; break; }
				if (set[94].Get(la.kind)) {
					goto case 508;
				} else {
					if (la.kind == 35) {
						currentState = 506;
						break;
					} else {
						goto case 6;
					}
				}
			}
			case 506: {
				stateStack.Push(507);
				goto case 508;
			}
			case 507: {
				if (la == null) { currentState = 507; break; }
				if (la.kind == 22) {
					currentState = 506;
					break;
				} else {
					goto case 83;
				}
			}
			case 508: {
				if (la == null) { currentState = 508; break; }
				if (set[16].Get(la.kind)) {
					currentState = 38;
					break;
				} else {
					if (la.kind == 162) {
						goto case 122;
					} else {
						if (la.kind == 84) {
							goto case 138;
						} else {
							if (la.kind == 209) {
								goto case 113;
							} else {
								goto case 6;
							}
						}
					}
				}
			}
			case 509: {
				if (la == null) { currentState = 509; break; }
				if (la.kind == 2) {
					goto case 146;
				} else {
					if (la.kind == 62) {
						goto case 144;
					} else {
						if (la.kind == 64) {
							goto case 143;
						} else {
							if (la.kind == 65) {
								goto case 142;
							} else {
								if (la.kind == 66) {
									goto case 141;
								} else {
									if (la.kind == 67) {
										goto case 140;
									} else {
										if (la.kind == 70) {
											goto case 139;
										} else {
											if (la.kind == 87) {
												goto case 137;
											} else {
												if (la.kind == 104) {
													goto case 135;
												} else {
													if (la.kind == 107) {
														goto case 134;
													} else {
														if (la.kind == 116) {
															goto case 132;
														} else {
															if (la.kind == 121) {
																goto case 131;
															} else {
																if (la.kind == 133) {
																	goto case 127;
																} else {
																	if (la.kind == 139) {
																		goto case 126;
																	} else {
																		if (la.kind == 143) {
																			goto case 125;
																		} else {
																			if (la.kind == 146) {
																				goto case 124;
																			} else {
																				if (la.kind == 147) {
																					goto case 123;
																				} else {
																					if (la.kind == 170) {
																						goto case 120;
																					} else {
																						if (la.kind == 176) {
																							goto case 119;
																						} else {
																							if (la.kind == 184) {
																								goto case 118;
																							} else {
																								if (la.kind == 203) {
																									goto case 115;
																								} else {
																									if (la.kind == 212) {
																										goto case 110;
																									} else {
																										if (la.kind == 213) {
																											goto case 109;
																										} else {
																											if (la.kind == 223) {
																												goto case 107;
																											} else {
																												if (la.kind == 224) {
																													goto case 106;
																												} else {
																													if (la.kind == 230) {
																														goto case 105;
																													} else {
																														goto case 6;
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			case 510: {
				PushContext(Context.Type, la, t);
				stateStack.Push(511);
				goto case 505;
			}
			case 511: {
				PopContext();
				goto case 497;
			}
			case 512: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(513);
				goto case 206;
			}
			case 513: {
				PopContext();
				goto case 514;
			}
			case 514: {
				if (la == null) { currentState = 514; break; }
				if (la.kind == 37) {
					currentState = 519;
					break;
				} else {
					goto case 515;
				}
			}
			case 515: {
				if (la == null) { currentState = 515; break; }
				if (la.kind == 63) {
					currentState = 516;
					break;
				} else {
					goto case 23;
				}
			}
			case 516: {
				PushContext(Context.Type, la, t);
				goto case 517;
			}
			case 517: {
				if (la == null) { currentState = 517; break; }
				if (la.kind == 40) {
					stateStack.Push(517);
					goto case 435;
				} else {
					stateStack.Push(518);
					goto case 37;
				}
			}
			case 518: {
				PopContext();
				goto case 23;
			}
			case 519: {
				SetIdentifierExpected(la);
				goto case 520;
			}
			case 520: {
				if (la == null) { currentState = 520; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(521);
					goto case 422;
				} else {
					goto case 521;
				}
			}
			case 521: {
				if (la == null) { currentState = 521; break; }
				Expect(38, la); // ")"
				currentState = 515;
				break;
			}
			case 522: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(523);
				goto case 206;
			}
			case 523: {
				PopContext();
				goto case 524;
			}
			case 524: {
				if (la == null) { currentState = 524; break; }
				if (la.kind == 37 || la.kind == 63) {
					if (la.kind == 63) {
						currentState = 529;
						break;
					} else {
						if (la.kind == 37) {
							currentState = 526;
							break;
						} else {
							goto case 525;
						}
					}
				} else {
					goto case 23;
				}
			}
			case 525: {
				Error(la);
				goto case 23;
			}
			case 526: {
				SetIdentifierExpected(la);
				goto case 527;
			}
			case 527: {
				if (la == null) { currentState = 527; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(528);
					goto case 422;
				} else {
					goto case 528;
				}
			}
			case 528: {
				if (la == null) { currentState = 528; break; }
				Expect(38, la); // ")"
				currentState = 23;
				break;
			}
			case 529: {
				PushContext(Context.Type, la, t);
				stateStack.Push(530);
				goto case 37;
			}
			case 530: {
				PopContext();
				goto case 23;
			}
			case 531: {
				PushContext(Context.TypeDeclaration, la, t);
				goto case 532;
			}
			case 532: {
				if (la == null) { currentState = 532; break; }
				Expect(115, la); // "Enum"
				currentState = 533;
				break;
			}
			case 533: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(534);
				goto case 206;
			}
			case 534: {
				PopContext();
				goto case 535;
			}
			case 535: {
				if (la == null) { currentState = 535; break; }
				if (la.kind == 63) {
					currentState = 547;
					break;
				} else {
					goto case 536;
				}
			}
			case 536: {
				stateStack.Push(537);
				goto case 23;
			}
			case 537: {
				SetIdentifierExpected(la);
				goto case 538;
			}
			case 538: {
				if (la == null) { currentState = 538; break; }
				if (set[97].Get(la.kind)) {
					goto case 542;
				} else {
					Expect(113, la); // "End"
					currentState = 539;
					break;
				}
			}
			case 539: {
				if (la == null) { currentState = 539; break; }
				Expect(115, la); // "Enum"
				currentState = 540;
				break;
			}
			case 540: {
				stateStack.Push(541);
				goto case 23;
			}
			case 541: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 542: {
				SetIdentifierExpected(la);
				goto case 543;
			}
			case 543: {
				if (la == null) { currentState = 543; break; }
				if (la.kind == 40) {
					stateStack.Push(542);
					goto case 435;
				} else {
					PushContext(Context.Identifier, la, t);
					SetIdentifierExpected(la);
					stateStack.Push(544);
					goto case 206;
				}
			}
			case 544: {
				PopContext();
				goto case 545;
			}
			case 545: {
				if (la == null) { currentState = 545; break; }
				if (la.kind == 20) {
					currentState = 546;
					break;
				} else {
					goto case 536;
				}
			}
			case 546: {
				stateStack.Push(536);
				goto case 56;
			}
			case 547: {
				PushContext(Context.Type, la, t);
				stateStack.Push(548);
				goto case 37;
			}
			case 548: {
				PopContext();
				goto case 536;
			}
			case 549: {
				if (la == null) { currentState = 549; break; }
				Expect(103, la); // "Delegate"
				currentState = 550;
				break;
			}
			case 550: {
				if (la == null) { currentState = 550; break; }
				if (la.kind == 210) {
					currentState = 551;
					break;
				} else {
					if (la.kind == 127) {
						currentState = 551;
						break;
					} else {
						Error(la);
						goto case 551;
					}
				}
			}
			case 551: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				goto case 552;
			}
			case 552: {
				if (la == null) { currentState = 552; break; }
				currentState = 553;
				break;
			}
			case 553: {
				PopContext();
				goto case 554;
			}
			case 554: {
				if (la == null) { currentState = 554; break; }
				if (la.kind == 37) {
					currentState = 557;
					break;
				} else {
					if (la.kind == 63) {
						currentState = 555;
						break;
					} else {
						goto case 23;
					}
				}
			}
			case 555: {
				PushContext(Context.Type, la, t);
				stateStack.Push(556);
				goto case 37;
			}
			case 556: {
				PopContext();
				goto case 23;
			}
			case 557: {
				SetIdentifierExpected(la);
				goto case 558;
			}
			case 558: {
				if (la == null) { currentState = 558; break; }
				if (set[151].Get(la.kind)) {
					if (la.kind == 169) {
						currentState = 560;
						break;
					} else {
						if (set[78].Get(la.kind)) {
							stateStack.Push(559);
							goto case 422;
						} else {
							Error(la);
							goto case 559;
						}
					}
				} else {
					goto case 559;
				}
			}
			case 559: {
				if (la == null) { currentState = 559; break; }
				Expect(38, la); // ")"
				currentState = 554;
				break;
			}
			case 560: {
				stateStack.Push(559);
				goto case 492;
			}
			case 561: {
				PushContext(Context.TypeDeclaration, la, t);
				goto case 562;
			}
			case 562: {
				if (la == null) { currentState = 562; break; }
				if (la.kind == 155) {
					currentState = 563;
					break;
				} else {
					if (la.kind == 84) {
						currentState = 563;
						break;
					} else {
						if (la.kind == 209) {
							currentState = 563;
							break;
						} else {
							Error(la);
							goto case 563;
						}
					}
				}
			}
			case 563: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(564);
				goto case 206;
			}
			case 564: {
				PopContext();
				goto case 565;
			}
			case 565: {
				if (la == null) { currentState = 565; break; }
				if (la.kind == 37) {
					currentState = 729;
					break;
				} else {
					goto case 566;
				}
			}
			case 566: {
				stateStack.Push(567);
				goto case 23;
			}
			case 567: {
				SetIdentifierExpected(la);
				isMissingModifier = true;
				goto case 568;
			}
			case 568: {
				if (la == null) { currentState = 568; break; }
				if (la.kind == 140) {
					isMissingModifier = false;
					goto case 726;
				} else {
					goto case 569;
				}
			}
			case 569: {
				SetIdentifierExpected(la);
				isMissingModifier = true;
				goto case 570;
			}
			case 570: {
				if (la == null) { currentState = 570; break; }
				if (la.kind == 136) {
					isMissingModifier = false;
					goto case 720;
				} else {
					goto case 571;
				}
			}
			case 571: {
				SetIdentifierExpected(la);
				isMissingModifier = true;
				goto case 572;
			}
			case 572: {
				if (la == null) { currentState = 572; break; }
				if (set[101].Get(la.kind)) {
					goto case 577;
				} else {
					isMissingModifier = false;
					goto case 573;
				}
			}
			case 573: {
				if (la == null) { currentState = 573; break; }
				Expect(113, la); // "End"
				currentState = 574;
				break;
			}
			case 574: {
				if (la == null) { currentState = 574; break; }
				if (la.kind == 155) {
					currentState = 575;
					break;
				} else {
					if (la.kind == 84) {
						currentState = 575;
						break;
					} else {
						if (la.kind == 209) {
							currentState = 575;
							break;
						} else {
							Error(la);
							goto case 575;
						}
					}
				}
			}
			case 575: {
				stateStack.Push(576);
				goto case 23;
			}
			case 576: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 577: {
				SetIdentifierExpected(la);
				isMissingModifier = true;
				goto case 578;
			}
			case 578: {
				if (la == null) { currentState = 578; break; }
				if (la.kind == 40) {
					stateStack.Push(577);
					goto case 435;
				} else {
					isMissingModifier = true;
					goto case 579;
				}
			}
			case 579: {
				SetIdentifierExpected(la);
				goto case 580;
			}
			case 580: {
				if (la == null) { currentState = 580; break; }
				if (set[132].Get(la.kind)) {
					currentState = 719;
					break;
				} else {
					isMissingModifier = false;
					SetIdentifierExpected(la);
					goto case 581;
				}
			}
			case 581: {
				if (la == null) { currentState = 581; break; }
				if (la.kind == 84 || la.kind == 155 || la.kind == 209) {
					stateStack.Push(571);
					goto case 561;
				} else {
					if (la.kind == 103) {
						stateStack.Push(571);
						goto case 549;
					} else {
						if (la.kind == 115) {
							stateStack.Push(571);
							goto case 531;
						} else {
							if (la.kind == 142) {
								stateStack.Push(571);
								goto case 9;
							} else {
								if (set[104].Get(la.kind)) {
									stateStack.Push(571);
									PushContext(Context.Member, la, t);
									SetIdentifierExpected(la);
									goto case 582;
								} else {
									Error(la);
									goto case 571;
								}
							}
						}
					}
				}
			}
			case 582: {
				if (la == null) { currentState = 582; break; }
				if (set[122].Get(la.kind)) {
					stateStack.Push(583);
					goto case 704;
				} else {
					if (la.kind == 127 || la.kind == 210) {
						stateStack.Push(583);
						goto case 686;
					} else {
						if (la.kind == 101) {
							stateStack.Push(583);
							goto case 670;
						} else {
							if (la.kind == 119) {
								stateStack.Push(583);
								goto case 655;
							} else {
								if (la.kind == 98) {
									stateStack.Push(583);
									goto case 643;
								} else {
									if (la.kind == 186) {
										stateStack.Push(583);
										goto case 598;
									} else {
										if (la.kind == 172) {
											stateStack.Push(583);
											goto case 584;
										} else {
											Error(la);
											goto case 583;
										}
									}
								}
							}
						}
					}
				}
			}
			case 583: {
				PopContext();
				currentState = stateStack.Pop();
				goto switchlbl;
			}
			case 584: {
				if (la == null) { currentState = 584; break; }
				Expect(172, la); // "Operator"
				currentState = 585;
				break;
			}
			case 585: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				goto case 586;
			}
			case 586: {
				if (la == null) { currentState = 586; break; }
				currentState = 587;
				break;
			}
			case 587: {
				PopContext();
				goto case 588;
			}
			case 588: {
				if (la == null) { currentState = 588; break; }
				Expect(37, la); // "("
				currentState = 589;
				break;
			}
			case 589: {
				stateStack.Push(590);
				goto case 422;
			}
			case 590: {
				if (la == null) { currentState = 590; break; }
				Expect(38, la); // ")"
				currentState = 591;
				break;
			}
			case 591: {
				if (la == null) { currentState = 591; break; }
				if (la.kind == 63) {
					currentState = 595;
					break;
				} else {
					goto case 592;
				}
			}
			case 592: {
				stateStack.Push(593);
				goto case 263;
			}
			case 593: {
				if (la == null) { currentState = 593; break; }
				Expect(113, la); // "End"
				currentState = 594;
				break;
			}
			case 594: {
				if (la == null) { currentState = 594; break; }
				Expect(172, la); // "Operator"
				currentState = 23;
				break;
			}
			case 595: {
				PushContext(Context.Type, la, t);
				goto case 596;
			}
			case 596: {
				if (la == null) { currentState = 596; break; }
				if (la.kind == 40) {
					stateStack.Push(596);
					goto case 435;
				} else {
					stateStack.Push(597);
					goto case 37;
				}
			}
			case 597: {
				PopContext();
				goto case 592;
			}
			case 598: {
				if (la == null) { currentState = 598; break; }
				Expect(186, la); // "Property"
				currentState = 599;
				break;
			}
			case 599: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(600);
				goto case 206;
			}
			case 600: {
				PopContext();
				goto case 601;
			}
			case 601: {
				if (la == null) { currentState = 601; break; }
				if (la.kind == 37) {
					currentState = 640;
					break;
				} else {
					goto case 602;
				}
			}
			case 602: {
				if (la == null) { currentState = 602; break; }
				if (la.kind == 63) {
					currentState = 638;
					break;
				} else {
					goto case 603;
				}
			}
			case 603: {
				if (la == null) { currentState = 603; break; }
				if (la.kind == 136) {
					currentState = 633;
					break;
				} else {
					goto case 604;
				}
			}
			case 604: {
				if (la == null) { currentState = 604; break; }
				if (la.kind == 20) {
					currentState = 632;
					break;
				} else {
					goto case 605;
				}
			}
			case 605: {
				stateStack.Push(606);
				goto case 23;
			}
			case 606: {
				PopContext();
				goto case 607;
			}
			case 607: {
				if (la == null) { currentState = 607; break; }
				if (la.kind == 40) {
					stateStack.Push(607);
					goto case 435;
				} else {
					goto case 608;
				}
			}
			case 608: {
				if (la == null) { currentState = 608; break; }
				if (set[152].Get(la.kind)) {
					currentState = 631;
					break;
				} else {
					if (la.kind == 128 || la.kind == 198) {
						PushContext(Context.Member, la, t);
						goto case 609;
					} else {
						currentState = stateStack.Pop();
						goto switchlbl;
					}
				}
			}
			case 609: {
				if (la == null) { currentState = 609; break; }
				if (la.kind == 128) {
					currentState = 610;
					break;
				} else {
					if (la.kind == 198) {
						currentState = 610;
						break;
					} else {
						Error(la);
						goto case 610;
					}
				}
			}
			case 610: {
				if (la == null) { currentState = 610; break; }
				if (la.kind == 37) {
					currentState = 628;
					break;
				} else {
					goto case 611;
				}
			}
			case 611: {
				stateStack.Push(612);
				goto case 263;
			}
			case 612: {
				if (la == null) { currentState = 612; break; }
				Expect(113, la); // "End"
				currentState = 613;
				break;
			}
			case 613: {
				if (la == null) { currentState = 613; break; }
				if (la.kind == 128) {
					currentState = 614;
					break;
				} else {
					if (la.kind == 198) {
						currentState = 614;
						break;
					} else {
						Error(la);
						goto case 614;
					}
				}
			}
			case 614: {
				stateStack.Push(615);
				goto case 23;
			}
			case 615: {
				if (la == null) { currentState = 615; break; }
				if (set[110].Get(la.kind)) {
					goto case 618;
				} else {
					goto case 616;
				}
			}
			case 616: {
				if (la == null) { currentState = 616; break; }
				Expect(113, la); // "End"
				currentState = 617;
				break;
			}
			case 617: {
				if (la == null) { currentState = 617; break; }
				Expect(186, la); // "Property"
				currentState = 23;
				break;
			}
			case 618: {
				if (la == null) { currentState = 618; break; }
				if (la.kind == 40) {
					stateStack.Push(618);
					goto case 435;
				} else {
					goto case 619;
				}
			}
			case 619: {
				if (la == null) { currentState = 619; break; }
				if (set[152].Get(la.kind)) {
					currentState = 619;
					break;
				} else {
					if (la.kind == 128) {
						currentState = 620;
						break;
					} else {
						if (la.kind == 198) {
							currentState = 620;
							break;
						} else {
							Error(la);
							goto case 620;
						}
					}
				}
			}
			case 620: {
				if (la == null) { currentState = 620; break; }
				if (la.kind == 37) {
					currentState = 625;
					break;
				} else {
					goto case 621;
				}
			}
			case 621: {
				stateStack.Push(622);
				goto case 263;
			}
			case 622: {
				if (la == null) { currentState = 622; break; }
				Expect(113, la); // "End"
				currentState = 623;
				break;
			}
			case 623: {
				if (la == null) { currentState = 623; break; }
				if (la.kind == 128) {
					currentState = 624;
					break;
				} else {
					if (la.kind == 198) {
						currentState = 624;
						break;
					} else {
						Error(la);
						goto case 624;
					}
				}
			}
			case 624: {
				stateStack.Push(616);
				goto case 23;
			}
			case 625: {
				SetIdentifierExpected(la);
				goto case 626;
			}
			case 626: {
				if (la == null) { currentState = 626; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(627);
					goto case 422;
				} else {
					goto case 627;
				}
			}
			case 627: {
				if (la == null) { currentState = 627; break; }
				Expect(38, la); // ")"
				currentState = 621;
				break;
			}
			case 628: {
				SetIdentifierExpected(la);
				goto case 629;
			}
			case 629: {
				if (la == null) { currentState = 629; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(630);
					goto case 422;
				} else {
					goto case 630;
				}
			}
			case 630: {
				if (la == null) { currentState = 630; break; }
				Expect(38, la); // ")"
				currentState = 611;
				break;
			}
			case 631: {
				SetIdentifierExpected(la);
				goto case 608;
			}
			case 632: {
				stateStack.Push(605);
				goto case 56;
			}
			case 633: {
				PushContext(Context.Type, la, t);
				stateStack.Push(634);
				goto case 37;
			}
			case 634: {
				PopContext();
				goto case 635;
			}
			case 635: {
				if (la == null) { currentState = 635; break; }
				if (la.kind == 22) {
					currentState = 636;
					break;
				} else {
					goto case 604;
				}
			}
			case 636: {
				PushContext(Context.Type, la, t);
				stateStack.Push(637);
				goto case 37;
			}
			case 637: {
				PopContext();
				goto case 635;
			}
			case 638: {
				if (la == null) { currentState = 638; break; }
				if (la.kind == 40) {
					stateStack.Push(638);
					goto case 435;
				} else {
					if (la.kind == 162) {
						stateStack.Push(603);
						goto case 86;
					} else {
						if (set[16].Get(la.kind)) {
							PushContext(Context.Type, la, t);
							stateStack.Push(639);
							goto case 37;
						} else {
							Error(la);
							goto case 603;
						}
					}
				}
			}
			case 639: {
				PopContext();
				goto case 603;
			}
			case 640: {
				SetIdentifierExpected(la);
				goto case 641;
			}
			case 641: {
				if (la == null) { currentState = 641; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(642);
					goto case 422;
				} else {
					goto case 642;
				}
			}
			case 642: {
				if (la == null) { currentState = 642; break; }
				Expect(38, la); // ")"
				currentState = 602;
				break;
			}
			case 643: {
				if (la == null) { currentState = 643; break; }
				Expect(98, la); // "Custom"
				currentState = 644;
				break;
			}
			case 644: {
				stateStack.Push(645);
				goto case 655;
			}
			case 645: {
				if (la == null) { currentState = 645; break; }
				if (set[115].Get(la.kind)) {
					goto case 647;
				} else {
					Expect(113, la); // "End"
					currentState = 646;
					break;
				}
			}
			case 646: {
				if (la == null) { currentState = 646; break; }
				Expect(119, la); // "Event"
				currentState = 23;
				break;
			}
			case 647: {
				if (la == null) { currentState = 647; break; }
				if (la.kind == 40) {
					stateStack.Push(647);
					goto case 435;
				} else {
					if (la.kind == 56) {
						currentState = 648;
						break;
					} else {
						if (la.kind == 193) {
							currentState = 648;
							break;
						} else {
							if (la.kind == 189) {
								currentState = 648;
								break;
							} else {
								Error(la);
								goto case 648;
							}
						}
					}
				}
			}
			case 648: {
				if (la == null) { currentState = 648; break; }
				Expect(37, la); // "("
				currentState = 649;
				break;
			}
			case 649: {
				stateStack.Push(650);
				goto case 422;
			}
			case 650: {
				if (la == null) { currentState = 650; break; }
				Expect(38, la); // ")"
				currentState = 651;
				break;
			}
			case 651: {
				stateStack.Push(652);
				goto case 263;
			}
			case 652: {
				if (la == null) { currentState = 652; break; }
				Expect(113, la); // "End"
				currentState = 653;
				break;
			}
			case 653: {
				if (la == null) { currentState = 653; break; }
				if (la.kind == 56) {
					currentState = 654;
					break;
				} else {
					if (la.kind == 193) {
						currentState = 654;
						break;
					} else {
						if (la.kind == 189) {
							currentState = 654;
							break;
						} else {
							Error(la);
							goto case 654;
						}
					}
				}
			}
			case 654: {
				stateStack.Push(645);
				goto case 23;
			}
			case 655: {
				if (la == null) { currentState = 655; break; }
				Expect(119, la); // "Event"
				currentState = 656;
				break;
			}
			case 656: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(657);
				goto case 206;
			}
			case 657: {
				PopContext();
				goto case 658;
			}
			case 658: {
				if (la == null) { currentState = 658; break; }
				if (la.kind == 63) {
					currentState = 668;
					break;
				} else {
					if (set[153].Get(la.kind)) {
						if (la.kind == 37) {
							currentState = 665;
							break;
						} else {
							goto case 659;
						}
					} else {
						Error(la);
						goto case 659;
					}
				}
			}
			case 659: {
				if (la == null) { currentState = 659; break; }
				if (la.kind == 136) {
					currentState = 660;
					break;
				} else {
					goto case 23;
				}
			}
			case 660: {
				PushContext(Context.Type, la, t);
				stateStack.Push(661);
				goto case 37;
			}
			case 661: {
				PopContext();
				goto case 662;
			}
			case 662: {
				if (la == null) { currentState = 662; break; }
				if (la.kind == 22) {
					currentState = 663;
					break;
				} else {
					goto case 23;
				}
			}
			case 663: {
				PushContext(Context.Type, la, t);
				stateStack.Push(664);
				goto case 37;
			}
			case 664: {
				PopContext();
				goto case 662;
			}
			case 665: {
				SetIdentifierExpected(la);
				goto case 666;
			}
			case 666: {
				if (la == null) { currentState = 666; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(667);
					goto case 422;
				} else {
					goto case 667;
				}
			}
			case 667: {
				if (la == null) { currentState = 667; break; }
				Expect(38, la); // ")"
				currentState = 659;
				break;
			}
			case 668: {
				PushContext(Context.Type, la, t);
				stateStack.Push(669);
				goto case 37;
			}
			case 669: {
				PopContext();
				goto case 659;
			}
			case 670: {
				if (la == null) { currentState = 670; break; }
				Expect(101, la); // "Declare"
				currentState = 671;
				break;
			}
			case 671: {
				if (la == null) { currentState = 671; break; }
				if (la.kind == 62 || la.kind == 66 || la.kind == 223) {
					currentState = 672;
					break;
				} else {
					goto case 672;
				}
			}
			case 672: {
				if (la == null) { currentState = 672; break; }
				if (la.kind == 210) {
					currentState = 673;
					break;
				} else {
					if (la.kind == 127) {
						currentState = 673;
						break;
					} else {
						Error(la);
						goto case 673;
					}
				}
			}
			case 673: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(674);
				goto case 206;
			}
			case 674: {
				PopContext();
				goto case 675;
			}
			case 675: {
				if (la == null) { currentState = 675; break; }
				Expect(149, la); // "Lib"
				currentState = 676;
				break;
			}
			case 676: {
				if (la == null) { currentState = 676; break; }
				Expect(3, la); // LiteralString
				currentState = 677;
				break;
			}
			case 677: {
				if (la == null) { currentState = 677; break; }
				if (la.kind == 59) {
					currentState = 685;
					break;
				} else {
					goto case 678;
				}
			}
			case 678: {
				if (la == null) { currentState = 678; break; }
				if (la.kind == 37) {
					currentState = 682;
					break;
				} else {
					goto case 679;
				}
			}
			case 679: {
				if (la == null) { currentState = 679; break; }
				if (la.kind == 63) {
					currentState = 680;
					break;
				} else {
					goto case 23;
				}
			}
			case 680: {
				PushContext(Context.Type, la, t);
				stateStack.Push(681);
				goto case 37;
			}
			case 681: {
				PopContext();
				goto case 23;
			}
			case 682: {
				SetIdentifierExpected(la);
				goto case 683;
			}
			case 683: {
				if (la == null) { currentState = 683; break; }
				if (set[78].Get(la.kind)) {
					stateStack.Push(684);
					goto case 422;
				} else {
					goto case 684;
				}
			}
			case 684: {
				if (la == null) { currentState = 684; break; }
				Expect(38, la); // ")"
				currentState = 679;
				break;
			}
			case 685: {
				if (la == null) { currentState = 685; break; }
				Expect(3, la); // LiteralString
				currentState = 678;
				break;
			}
			case 686: {
				if (la == null) { currentState = 686; break; }
				if (la.kind == 210) {
					currentState = 687;
					break;
				} else {
					if (la.kind == 127) {
						currentState = 687;
						break;
					} else {
						Error(la);
						goto case 687;
					}
				}
			}
			case 687: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				goto case 688;
			}
			case 688: {
				if (la == null) { currentState = 688; break; }
				currentState = 689;
				break;
			}
			case 689: {
				PopContext();
				goto case 690;
			}
			case 690: {
				if (la == null) { currentState = 690; break; }
				if (la.kind == 37) {
					currentState = 700;
					break;
				} else {
					if (la.kind == 63) {
						currentState = 698;
						break;
					} else {
						goto case 691;
					}
				}
			}
			case 691: {
				if (la == null) { currentState = 691; break; }
				if (la.kind == 134 || la.kind == 136) {
					currentState = 695;
					break;
				} else {
					goto case 692;
				}
			}
			case 692: {
				stateStack.Push(693);
				goto case 263;
			}
			case 693: {
				if (la == null) { currentState = 693; break; }
				Expect(113, la); // "End"
				currentState = 694;
				break;
			}
			case 694: {
				if (la == null) { currentState = 694; break; }
				if (la.kind == 210) {
					currentState = 23;
					break;
				} else {
					if (la.kind == 127) {
						currentState = 23;
						break;
					} else {
						goto case 525;
					}
				}
			}
			case 695: {
				if (la == null) { currentState = 695; break; }
				if (la.kind == 153 || la.kind == 158 || la.kind == 159) {
					currentState = 697;
					break;
				} else {
					goto case 696;
				}
			}
			case 696: {
				stateStack.Push(692);
				goto case 37;
			}
			case 697: {
				if (la == null) { currentState = 697; break; }
				Expect(26, la); // "."
				currentState = 696;
				break;
			}
			case 698: {
				PushContext(Context.Type, la, t);
				stateStack.Push(699);
				goto case 37;
			}
			case 699: {
				PopContext();
				goto case 691;
			}
			case 700: {
				SetIdentifierExpected(la);
				goto case 701;
			}
			case 701: {
				if (la == null) { currentState = 701; break; }
				if (set[151].Get(la.kind)) {
					if (la.kind == 169) {
						currentState = 703;
						break;
					} else {
						if (set[78].Get(la.kind)) {
							stateStack.Push(702);
							goto case 422;
						} else {
							Error(la);
							goto case 702;
						}
					}
				} else {
					goto case 702;
				}
			}
			case 702: {
				if (la == null) { currentState = 702; break; }
				Expect(38, la); // ")"
				currentState = 690;
				break;
			}
			case 703: {
				stateStack.Push(702);
				goto case 492;
			}
			case 704: {
				stateStack.Push(705);
				SetIdentifierExpected(la);
				goto case 706;
			}
			case 705: {
				if (la == null) { currentState = 705; break; }
				if (la.kind == 22) {
					currentState = 704;
					break;
				} else {
					goto case 23;
				}
			}
			case 706: {
				if (la == null) { currentState = 706; break; }
				if (la.kind == 88) {
					currentState = 707;
					break;
				} else {
					goto case 707;
				}
			}
			case 707: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				stateStack.Push(708);
				goto case 718;
			}
			case 708: {
				PopContext();
				goto case 709;
			}
			case 709: {
				if (la == null) { currentState = 709; break; }
				if (la.kind == 33) {
					currentState = 710;
					break;
				} else {
					goto case 710;
				}
			}
			case 710: {
				if (la == null) { currentState = 710; break; }
				if (la.kind == 37) {
					currentState = 715;
					break;
				} else {
					if (la.kind == 63) {
						currentState = 712;
						break;
					} else {
						goto case 711;
					}
				}
			}
			case 711: {
				if (la == null) { currentState = 711; break; }
				if (la.kind == 20) {
					goto case 406;
				} else {
					currentState = stateStack.Pop();
					goto switchlbl;
				}
			}
			case 712: {
				PushContext(Context.Type, la, t);
				goto case 713;
			}
			case 713: {
				if (la == null) { currentState = 713; break; }
				if (la.kind == 162) {
					stateStack.Push(714);
					goto case 86;
				} else {
					if (set[16].Get(la.kind)) {
						stateStack.Push(714);
						goto case 37;
					} else {
						Error(la);
						goto case 714;
					}
				}
			}
			case 714: {
				PopContext();
				goto case 711;
			}
			case 715: {
				nextTokenIsPotentialStartOfExpression = true;
				goto case 716;
			}
			case 716: {
				if (la == null) { currentState = 716; break; }
				if (set[23].Get(la.kind)) {
					stateStack.Push(717);
					goto case 56;
				} else {
					goto case 717;
				}
			}
			case 717: {
				if (la == null) { currentState = 717; break; }
				if (la.kind == 22) {
					currentState = 715;
					break;
				} else {
					Expect(38, la); // ")"
					currentState = 710;
					break;
				}
			}
			case 718: {
				if (la == null) { currentState = 718; break; }
				if (set[137].Get(la.kind)) {
					currentState = stateStack.Pop();
					break;
				} else {
					if (la.kind == 58) {
						goto case 145;
					} else {
						if (la.kind == 126) {
							goto case 129;
						} else {
							goto case 6;
						}
					}
				}
			}
			case 719: {
				isMissingModifier = false;
				goto case 579;
			}
			case 720: {
				if (la == null) { currentState = 720; break; }
				Expect(136, la); // "Implements"
				currentState = 721;
				break;
			}
			case 721: {
				PushContext(Context.Type, la, t);
				stateStack.Push(722);
				goto case 37;
			}
			case 722: {
				PopContext();
				goto case 723;
			}
			case 723: {
				if (la == null) { currentState = 723; break; }
				if (la.kind == 22) {
					currentState = 724;
					break;
				} else {
					stateStack.Push(571);
					goto case 23;
				}
			}
			case 724: {
				PushContext(Context.Type, la, t);
				stateStack.Push(725);
				goto case 37;
			}
			case 725: {
				PopContext();
				goto case 723;
			}
			case 726: {
				if (la == null) { currentState = 726; break; }
				Expect(140, la); // "Inherits"
				currentState = 727;
				break;
			}
			case 727: {
				PushContext(Context.Type, la, t);
				stateStack.Push(728);
				goto case 37;
			}
			case 728: {
				PopContext();
				stateStack.Push(569);
				goto case 23;
			}
			case 729: {
				if (la == null) { currentState = 729; break; }
				Expect(169, la); // "Of"
				currentState = 730;
				break;
			}
			case 730: {
				stateStack.Push(731);
				goto case 492;
			}
			case 731: {
				if (la == null) { currentState = 731; break; }
				Expect(38, la); // ")"
				currentState = 566;
				break;
			}
			case 732: {
				isMissingModifier = false;
				goto case 28;
			}
			case 733: {
				PushContext(Context.Type, la, t);
				stateStack.Push(734);
				goto case 37;
			}
			case 734: {
				PopContext();
				goto case 735;
			}
			case 735: {
				if (la == null) { currentState = 735; break; }
				if (la.kind == 22) {
					currentState = 736;
					break;
				} else {
					stateStack.Push(17);
					goto case 23;
				}
			}
			case 736: {
				PushContext(Context.Type, la, t);
				stateStack.Push(737);
				goto case 37;
			}
			case 737: {
				PopContext();
				goto case 735;
			}
			case 738: {
				if (la == null) { currentState = 738; break; }
				Expect(169, la); // "Of"
				currentState = 739;
				break;
			}
			case 739: {
				stateStack.Push(740);
				goto case 492;
			}
			case 740: {
				if (la == null) { currentState = 740; break; }
				Expect(38, la); // ")"
				currentState = 14;
				break;
			}
			case 741: {
				PushContext(Context.Identifier, la, t);
				SetIdentifierExpected(la);
				goto case 742;
			}
			case 742: {
				if (la == null) { currentState = 742; break; }
				if (set[50].Get(la.kind)) {
					currentState = 742;
					break;
				} else {
					PopContext();
					stateStack.Push(743);
					goto case 23;
				}
			}
			case 743: {
				if (la == null) { currentState = 743; break; }
				if (set[3].Get(la.kind)) {
					stateStack.Push(743);
					goto case 5;
				} else {
					Expect(113, la); // "End"
					currentState = 744;
					break;
				}
			}
			case 744: {
				if (la == null) { currentState = 744; break; }
				Expect(160, la); // "Namespace"
				currentState = 23;
				break;
			}
			case 745: {
				if (la == null) { currentState = 745; break; }
				Expect(137, la); // "Imports"
				currentState = 746;
				break;
			}
			case 746: {
				PushContext(Context.Importable, la, t);
				nextTokenIsStartOfImportsOrAccessExpression = true;	
				goto case 747;
			}
			case 747: {
				if (la == null) { currentState = 747; break; }
				if (set[154].Get(la.kind)) {
					currentState = 753;
					break;
				} else {
					if (la.kind == 10) {
						currentState = 749;
						break;
					} else {
						Error(la);
						goto case 748;
					}
				}
			}
			case 748: {
				PopContext();
				goto case 23;
			}
			case 749: {
				stateStack.Push(750);
				goto case 206;
			}
			case 750: {
				if (la == null) { currentState = 750; break; }
				Expect(20, la); // "="
				currentState = 751;
				break;
			}
			case 751: {
				if (la == null) { currentState = 751; break; }
				Expect(3, la); // LiteralString
				currentState = 752;
				break;
			}
			case 752: {
				if (la == null) { currentState = 752; break; }
				Expect(11, la); // XmlCloseTag
				currentState = 748;
				break;
			}
			case 753: {
				if (la == null) { currentState = 753; break; }
				if (la.kind == 37) {
					stateStack.Push(753);
					goto case 42;
				} else {
					if (la.kind == 20 || la.kind == 26) {
						currentState = 754;
						break;
					} else {
						goto case 748;
					}
				}
			}
			case 754: {
				stateStack.Push(748);
				goto case 37;
			}
			case 755: {
				if (la == null) { currentState = 755; break; }
				Expect(173, la); // "Option"
				currentState = 756;
				break;
			}
			case 756: {
				if (la == null) { currentState = 756; break; }
				if (la.kind == 121 || la.kind == 139 || la.kind == 207) {
					currentState = 758;
					break;
				} else {
					if (la.kind == 87) {
						currentState = 757;
						break;
					} else {
						goto case 525;
					}
				}
			}
			case 757: {
				if (la == null) { currentState = 757; break; }
				if (la.kind == 213) {
					currentState = 23;
					break;
				} else {
					if (la.kind == 67) {
						currentState = 23;
						break;
					} else {
						goto case 525;
					}
				}
			}
			case 758: {
				if (la == null) { currentState = 758; break; }
				if (la.kind == 170 || la.kind == 171) {
					currentState = 23;
					break;
				} else {
					goto case 23;
				}
			}
		}

		if (la != null) {
			t = la;
			nextTokenIsPotentialStartOfExpression = false;
			readXmlIdentifier = false;
			nextTokenIsStartOfImportsOrAccessExpression = false;
			wasQualifierTokenAtStart = false;
			identifierExpected = false;
		}
	}
	
	public void Advance()
	{
		//Console.WriteLine("Advance");
		InformToken(null);
	}
	
	public BitArray GetExpectedSet() { return GetExpectedSet(currentState); }
	
	static readonly BitArray[] set = {
		new BitArray(new int[] {1, 256, 1048576, 537395328, 402670080, 444604481, 131200, 0}),
		new BitArray(new int[] {1, 256, 1048576, 537395328, 402670080, 444596289, 131200, 0}),
		new BitArray(new int[] {1, 256, 1048576, 537395328, 402669568, 444596289, 131200, 0}),
		new BitArray(new int[] {0, 256, 1048576, 537395328, 402669568, 444596289, 131200, 0}),
		new BitArray(new int[] {0, 256, 1048576, 537395328, 402669568, 444596288, 131200, 0}),
		new BitArray(new int[] {0, 0, 1048576, 537395328, 402669568, 444596288, 131200, 0}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {0, 256, 1048576, -1601568064, 671109120, 1589117122, 393600, 3328}),
		new BitArray(new int[] {0, 256, 1048576, -1601568064, 671105024, 1589117122, 393600, 3328}),
		new BitArray(new int[] {5, 1140850944, 26214479, -493220892, 940361760, 1606227139, -2143942272, 3393}),
		new BitArray(new int[] {0, 256, 1048576, -1601699136, 671105024, 1589117122, 393600, 3328}),
		new BitArray(new int[] {0, 0, 1048576, -1601699136, 671105024, 1589117122, 393600, 3328}),
		new BitArray(new int[] {0, 0, 1048576, -2138570624, 134234112, 67108864, 393216, 0}),
		new BitArray(new int[] {0, 0, 0, -2139095040, 0, 67108864, 262144, 0}),
		new BitArray(new int[] {-2, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {2097154, -2147483616, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850690, 8650975, 1108355356, 9218084, 17106176, -533656048, 67}),
		new BitArray(new int[] {-940564478, 889192445, 65, 1074825472, 72844640, 231424, 22030368, 4704}),
		new BitArray(new int[] {-940564478, 889192413, 65, 1074825472, 72844640, 231424, 22030368, 4704}),
		new BitArray(new int[] {4, -16777216, -1, -1, -1, -1, -1, 16383}),
		new BitArray(new int[] {-61995012, 1174405224, -51384097, -972018405, -1030969182, 17106740, -97186288, 8259}),
		new BitArray(new int[] {-61995012, 1174405224, -51384097, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {-61995012, 1174405160, -51384097, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {-66189316, 1174405160, -51384097, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {-1007673342, 889192405, 65, 1074825472, 72843296, 231424, 22030368, 4160}),
		new BitArray(new int[] {-1013972992, 822083461, 0, 0, 71499776, 163840, 16777216, 4096}),
		new BitArray(new int[] {-66189316, 1174405176, -51384097, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {4, 1140850690, 8650975, 1108355356, 9218084, 17106176, -533656048, 579}),
		new BitArray(new int[] {-1007673342, 889192405, 65, 1074825472, 72843552, 231424, 22030368, 4160}),
		new BitArray(new int[] {-1007673342, 889192413, 65, 1074825472, 72843552, 231424, 22030368, 4672}),
		new BitArray(new int[] {-2, -9, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-1040382, 889192437, 65, 1074825472, 72843296, 231424, 22030368, 4160}),
		new BitArray(new int[] {1006632960, 32, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {1028, -16777216, -1, -1, -1, -1, -1, 16383}),
		new BitArray(new int[] {-1038334, -1258291211, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {1007552508, 1140850720, -51384097, -972018405, -1030969182, 17106208, -365621744, 8259}),
		new BitArray(new int[] {-1040382, -1258291209, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {0, 0, -60035072, 1027, 0, 0, 134217728, 0}),
		new BitArray(new int[] {0, 67108864, 0, 1073743872, 1310752, 65536, 1050656, 64}),
		new BitArray(new int[] {4194304, 67108864, 0, 1073743872, 1343520, 65536, 1050656, 64}),
		new BitArray(new int[] {-66189316, 1174405160, -51384097, -972018401, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {4194304, 67108864, 64, 1073743872, 1343520, 65536, 1050656, 64}),
		new BitArray(new int[] {66189314, -1174405161, 51384096, 972018404, 1030969181, -17106229, 97186287, -8260}),
		new BitArray(new int[] {65140738, 973078487, 51384096, 972018404, 1030969181, -17106229, 97186287, -8260}),
		new BitArray(new int[] {-66189316, 1174405160, -51384097, -972018405, -1030969182, 17106228, -97186288, 8387}),
		new BitArray(new int[] {0, 67108864, 0, 1073743872, 1343520, 65536, 1050656, 64}),
		new BitArray(new int[] {4, 1140851008, 8388975, 1108347140, 821280, 21316608, -2144335872, 65}),
		new BitArray(new int[] {-64092162, -973078488, -51384097, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {-64092162, 1191182376, -1048865, -546062565, -1014191950, -1593504452, -21144002, 8903}),
		new BitArray(new int[] {0, 0, 3072, 134447104, 16777216, 8, 0, 0}),
		new BitArray(new int[] {-2097156, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-66189316, 1191182376, -1051937, -680509669, -1030969166, -1593504460, -21144002, 8903}),
		new BitArray(new int[] {-66189316, 1174405162, -51384097, -972018401, -1030969178, 17106228, -97186288, 8259}),
		new BitArray(new int[] {6291458, 0, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {-64092162, 1174405160, -51384097, -971985637, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {0, 0, 0, -1879044096, 0, 67108864, 67371040, 128}),
		new BitArray(new int[] {36, 1140850688, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {2097158, 1140850688, 8388687, 1108347140, 821280, 17105920, -2144335872, 97}),
		new BitArray(new int[] {2097154, -2147483648, 0, 0, 0, 0, 0, 32}),
		new BitArray(new int[] {36, 1140850688, 8388687, 1108347140, 821280, 17105928, -2144335872, 65}),
		new BitArray(new int[] {-66189316, 1174405160, -51384097, -972018405, -1030969166, 17106228, -97186284, 8259}),
		new BitArray(new int[] {1007552508, 1140850720, -51384097, -972002021, -1030969182, 17106208, -365621744, 8259}),
		new BitArray(new int[] {1007681536, -2147483614, 0, 0, 1024, 0, 0, 0}),
		new BitArray(new int[] {1007681536, -2147483616, 0, 0, 1024, 0, 0, 0}),
		new BitArray(new int[] {2097154, 0, 0, 0, 0, 0, 0, 129}),
		new BitArray(new int[] {2097154, 0, 0, 32768, 0, 0, 0, 129}),
		new BitArray(new int[] {-66189316, 1174405160, -51383073, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {-65140740, 1174409128, -51384097, -971985637, -1030903646, 17106228, -97186288, 8259}),
		new BitArray(new int[] {-65140740, 1174409128, -51384097, -972018405, -1030903646, 17106228, -97186288, 8259}),
		new BitArray(new int[] {1048576, 3968, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {-64092162, 1191182376, -1051937, -680509669, -1030969166, -1593504460, -21144002, 8903}),
		new BitArray(new int[] {-64092162, 1191182376, -1051937, -680476901, -1030969166, -1593504460, -21144002, 8903}),
		new BitArray(new int[] {2097154, 32, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {7340034, -2147483614, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {7340034, -2147483616, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {7340034, -2147483648, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {3145730, 0, 0, 32768, 0, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850690, 8650975, 1108355356, 9218084, 17106180, -533656048, 67}),
		new BitArray(new int[] {4, 1140850944, 8388975, 1108347140, 821280, 21316608, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850688, 8388975, 1108347140, 821280, 21316608, -2144335872, 65}),
		new BitArray(new int[] {5242880, -2147483584, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {-2, -1, -3, -1, -134217729, -1, -1, -1}),
		new BitArray(new int[] {7, 1157628162, 26477055, -493212676, 948758565, 2147308999, -533262382, 3395}),
		new BitArray(new int[] {918528, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {-909310, -1258291209, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {-843774, -1258291209, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {-318462, -1258291209, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {-383998, -1258291209, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {-1038334, -1258291209, 65, 1074825472, 72844320, 231424, 22030368, 4160}),
		new BitArray(new int[] {4194308, 1140850754, 8650975, 1108355356, 9218084, 17106176, -533656048, 67}),
		new BitArray(new int[] {4, 1140851008, 8388975, 1108347140, 821280, 21317120, -2144335872, 65}),
		new BitArray(new int[] {4, 1073741824, 8388687, 34605312, 822304, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1073741824, 8388687, 34605312, 821280, 16843776, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850698, 9699551, 1108355356, 9218084, 17106180, -533524976, 67}),
		new BitArray(new int[] {4, 1140850690, 9699551, 1108355356, 9218084, 17106180, -533524976, 67}),
		new BitArray(new int[] {4, 1140850946, 8650975, 1108355356, 9218084, 17106176, -533656048, 67}),
		new BitArray(new int[] {4, 1140850944, 8388687, 1108478212, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850944, 8388687, 1108347140, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850944, 26214479, -493220892, 671930656, 1606227138, -2143942272, 3393}),
		new BitArray(new int[] {4, 1140850944, 26214479, -493220892, 671926560, 1606227138, -2143942272, 3393}),
		new BitArray(new int[] {4, 1140850944, 26214479, -493220892, 671926304, 1606227138, -2143942272, 3393}),
		new BitArray(new int[] {4, 1140850944, 26214479, -493351964, 671926304, 1606227138, -2143942272, 3393}),
		new BitArray(new int[] {4, 1140850688, 26214479, -493351964, 671926304, 1606227138, -2143942272, 3393}),
		new BitArray(new int[] {4, 1140850688, 26214479, -1030223452, 135055392, 84218880, -2143942656, 65}),
		new BitArray(new int[] {4, 1140850688, 25165903, -1030747868, 821280, 84218880, -2144073728, 65}),
		new BitArray(new int[] {3145730, -2147483616, 0, 0, 256, 0, 0, 0}),
		new BitArray(new int[] {3145730, -2147483648, 0, 0, 256, 0, 0, 0}),
		new BitArray(new int[] {3145730, 0, 0, 0, 256, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850944, 26214479, -493220892, 671926305, 1606227138, -2143942208, 3393}),
		new BitArray(new int[] {0, 256, 0, 537001984, 1, 436207616, 64, 0}),
		new BitArray(new int[] {0, 256, 0, 536870912, 1, 436207616, 64, 0}),
		new BitArray(new int[] {0, 0, 0, 536870912, 1, 436207616, 64, 0}),
		new BitArray(new int[] {7340034, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850946, 8650975, 1108355356, 9218084, 17106180, -533656048, 67}),
		new BitArray(new int[] {0, 16777472, 0, 131072, 0, 536870912, 2, 0}),
		new BitArray(new int[] {0, 16777472, 0, 0, 0, 536870912, 2, 0}),
		new BitArray(new int[] {2097154, -2147483616, 0, 0, 256, 0, 0, 0}),
		new BitArray(new int[] {0, 1073741824, 4, -2147483648, 0, 0, -2147221504, 0}),
		new BitArray(new int[] {2097154, -2013265888, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {2097154, -2147483616, 0, 0, 320, 0, 0, 0}),
		new BitArray(new int[] {2097154, 0, 0, 0, 320, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850690, 8650975, 1108355356, -1030969308, 17106176, -533656048, 67}),
		new BitArray(new int[] {4, 1140850688, 25165903, 1108347136, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {4, 1140850688, 8388687, 1108347136, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {7340034, -2147483614, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {7340034, -2147483616, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {0, 256, 1048576, 537526400, 402669568, 444596289, 131200, 0}),
		new BitArray(new int[] {1028, 1140850688, 8650975, 1108355356, 9218084, 17106176, -533656048, 67}),
		new BitArray(new int[] {70254594, 32, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {0, 0, 8388608, 33554432, 2048, 0, 32768, 0}),
		new BitArray(new int[] {2097154, 0, 0, 0, 0, 3072, 0, 0}),
		new BitArray(new int[] {0, 0, 0, 536870912, 268435456, 444596288, 128, 0}),
		new BitArray(new int[] {0, 0, 0, 536871488, 536870912, 1522008258, 384, 3328}),
		new BitArray(new int[] {0, 0, 262288, 8216, 8396800, 256, 1610679824, 2}),
		new BitArray(new int[] {-1073741824, 33554432, 0, 0, 0, 16, 0, 0}),
		new BitArray(new int[] {1006632960, 0, 0, 0, 0, 0, 0, 0}),
		new BitArray(new int[] {1016, 0, 0, 67108864, -1040187392, 32, 33554432, 0}),
		new BitArray(new int[] {4, 1073741824, 8388687, 34605312, 821280, 17105920, -2144335872, 65}),
		new BitArray(new int[] {0, 0, -1133776896, 3, 0, 0, 0, 0}),
		new BitArray(new int[] {-64092162, 1191182376, -1051937, -680378597, -1030969166, -1593504460, -21144002, 8903}),
		new BitArray(new int[] {0, 0, 33554432, 16777216, 16, 0, 16392, 0}),
		new BitArray(new int[] {-66189316, 1174405160, -51383585, -972018405, -1030969182, 17106228, -97186288, 8259}),
		new BitArray(new int[] {1048576, 3968, 0, 0, 65536, 0, 0, 0}),
		new BitArray(new int[] {0, 0, 288, 0, 0, 4210688, 0, 0}),
		new BitArray(new int[] {-2, -129, -3, -1, -134217729, -1, -1, -1}),
		new BitArray(new int[] {-18434, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-22530, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-32770, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-37890, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-2050, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {-6146, -1, -1, -1, -1, -1, -1, -1}),
		new BitArray(new int[] {4, 1140850944, 8388975, 1108347140, 821280, 21317120, -2144335872, 65}),
		new BitArray(new int[] {0, 0, 0, 536870912, 0, 436207616, 0, 0}),
		new BitArray(new int[] {2097154, 32, 0, 0, 256, 0, 0, 0}),
		new BitArray(new int[] {4, 1140850688, 8650975, 1108355356, 9218084, 17106176, -533656048, 67})

	};

} // end Parser


}