window.numberToWords = function numberToWords(number) {
    const numericValue = Number(number);
    if (!Number.isFinite(numericValue)) {
        return "";
    }

    number = Math.trunc(numericValue);

    if (number === 0) {
        return "صفر";
    }

    if (number < 0) {
        return "منفی " + numberToWords(Math.abs(number));
    }

    let words = "";

    if (Math.floor(number / 1000000000000000000) > 0) {
        if (Math.floor(number / 1000000000000000000) >= 2) {
            words += numberToWords(Math.floor(number / 1000000000000000000)) + " تریلیونه ";
            number %= 1000000000000000000;
        } else {
            words += " یو تریلیون ";
            number %= 1000000000000000000;
        }
    }

    if (Math.floor(number / 1000000000000000) > 0) {
        if (Math.floor(number / 1000000000000000) >= 2) {
            words += numberToWords(Math.floor(number / 1000000000000000)) + " بیلیارډه ";
            number %= 1000000000000000;
        } else {
            words += " یو بیلیارډ ";
            number %= 1000000000000000;
        }
    }

    if (Math.floor(number / 1000000000000) > 0) {
        if (Math.floor(number / 1000000000000) >= 2) {
            words += numberToWords(Math.floor(number / 1000000000000)) + " بیلیونه ";
            number %= 1000000000000;
        } else {
            words += " یو بیلیون ";
            number %= 1000000000000;
        }
    }

    if (Math.floor(number / 1000000000) > 0) {
        if (Math.floor(number / 1000000000) >= 2) {
            words += numberToWords(Math.floor(number / 1000000000)) + " میلیارده ";
            number %= 1000000000;
        } else {
            words += " یو میلیارد ";
            number %= 1000000000;
        }
    }

    if (Math.floor(number / 1000000) > 0) {
        if (Math.floor(number / 1000000) >= 2) {
            words += numberToWords(Math.floor(number / 1000000)) + " ملیونه ";
            number %= 1000000;
        } else {
            words += " یو ملیون ";
            number %= 1000000;
        }
    }

    if (Math.floor(number / 1000) > 0) {
        if (Math.floor(number / 1000) >= 2) {
            words += numberToWords(Math.floor(number / 1000)) + " زره ";
            number %= 1000;
        } else {
            number %= 1000;
            words += number > 0 ? " یو زر " : " زر ";
        }
    }

    if (Math.floor(number / 100) > 0) {
        if (Math.floor(number / 100) >= 2) {
            words += numberToWords(Math.floor(number / 100)) + " سوه ";
            number %= 100;
        } else {
            number %= 100;
            words += number > 0 ? " یو سل " : " سل ";
        }
    }

    if (number > 0) {
        if (words !== "") {
            words += "او ";
        }

        const unitsMap = [
            "صفر", "یو", "دوه", "درې", "څلور", "پنځه", "شپږ", "اووه", "اته", "نه",
            "لس", "یوولس", "دولس", "دیارلس", "څوارلس", "پنځلس", "شپاړلس", "اوولس", "اتلس", "نولس",
            "شل", "یوویشت", "دوه ویشت", "درویشت", "څلورویشت", "پنځه ویشت", "شپږویشت", "اوه ویشت", "اته ویشت", "نه ویشت"
        ];

        const tensMap = [
            "صفر", "لس", "شل", "دیرش", "څلویښت", "پنځوس", "شپیته", "اویا", "اتیا", "نوی"
        ];

        if (number < 30) {
            words += unitsMap[number];
        } else {
            let lastWords = "";
            const lastTens = tensMap[Math.floor(number / 10)];
            if ((number % 10) > 0) {
                lastWords += unitsMap[number % 10];
            }
            lastWords += " " + lastTens;
            words += lastWords;
        }
    }

    return words.replace(/\s+/g, " ").trim();
};

window.humanizeNumberValue = function humanizeNumberValue(value) {
    const numericValue = Number(value);
    if (!Number.isFinite(numericValue)) {
        return "صفر";
    }

    const absoluteValue = Math.abs(numericValue);
    const integerPart = Math.trunc(absoluteValue);
    const fractionalPart = Math.round((absoluteValue - integerPart) * 100);
    let words = window.numberToWords(integerPart);

    if (fractionalPart > 0) {
        words += ` اعشاریه ${window.numberToWords(fractionalPart)}`;
    }

    if (numericValue < 0) {
        words = `منفی ${words}`;
    }

    return words.replace(/\s+/g, " ").trim();
};
