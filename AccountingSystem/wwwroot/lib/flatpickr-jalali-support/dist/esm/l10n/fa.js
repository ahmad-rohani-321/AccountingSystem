import JalaliDate from "./jalaliDate";
var fp = typeof window !== "undefined" && window.flatpickr !== undefined
    ? window.flatpickr
    : {
        l10ns: {},
    };
function getWeek(givenDate) {
    var date = new JalaliDate(givenDate.getTime());
    date.setHours(0, 0, 0, 0);
    var week1 = new JalaliDate(givenDate.getFullYear(), 0, 1);
    var week2 = new JalaliDate(givenDate.getFullYear(), 0, (week1.getDay() + 6) % 7, 0, 0, 0);
    var weeks = 1 + Math.round((date.getTime() - week2.getTime()) / 86400000 / 7);
    if (weeks > 0)
        return weeks;
    return getWeek(new JalaliDate(givenDate.getFullYear(), 0, 0));
}
export var Persian = {
    weekdays: {
        shorthand: ["ی", "د", "س", "چ", "پ", "ج", "ش"],
        longhand: [
            "یک‌شنبه",
            "دوشنبه",
            "سه‌شنبه",
            "چهارشنبه",
            "پنچ‌شنبه",
            "جمعه",
            "شنبه",
        ],
    },
    months: {
        shorthand: [
            "فروردین",
            "اردیبهشت",
            "خرداد",
            "تیر",
            "مرداد",
            "شهریور",
            "مهر",
            "آبان",
            "آذر",
            "دی",
            "بهمن",
            "اسفند",
        ],
        longhand: [
            "فروردین",
            "اردیبهشت",
            "خرداد",
            "تیر",
            "مرداد",
            "شهریور",
            "مهر",
            "آبان",
            "آذر",
            "دی",
            "بهمن",
            "اسفند",
        ],
    },
    daysInMonth: [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29],
    isLeap: function (month, year) {
        return (month === 11 && [1, 5, 9, 13, 17, 22, 26, 30].indexOf(year % 33) > -1);
    },
    firstDayOfWeek: 6,
    ordinal: function () {
        return "";
    },
    rangeSeparator: " تا ",
    weekAbbreviation: "هفته",
    scrollTitle: "اسکرول نمایید",
    toggleTitle: "برای تغییر کلیک نمایید",
    amPM: ["صبح", "عصر"],
    yearAriaLabel: "سال",
    monthAriaLabel: "ماه",
    hourAriaLabel: "ساعت",
    minuteAriaLabel: "دقیقه",
    time_24hr: false,
    getWeek: getWeek,
    date: JalaliDate,
};
fp.l10ns.fa = Persian;
export default fp.l10ns;
