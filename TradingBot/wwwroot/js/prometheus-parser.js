/**
 * Парсер Prometheus exposition format для TradingBot UI.
 * Поддерживает метрики с labels: metric_name{label="value"} 123
 */
window.PrometheusParser = {
    /**
     * Парсит текст метрик Prometheus в объект { metricBaseName: value }.
     * Для метрик с labels суммирует значения по базовому имени (для counter) или берёт последнее (для gauge).
     */
    parse(metricsText) {
        const result = {};
        const lines = metricsText.split('\n');

        for (const line of lines) {
            if (line.startsWith('#') || line.trim() === '') continue;

            // Формат: metric_name{label="value"} value [timestamp]  или  metric_name value [timestamp]
            const match = line.match(/^([a-zA-Z_:][a-zA-Z0-9_:]*(?:\{[^}]*\})?)\s+([+-]?[\d.eE+-]+)(?:\s+(\d+))?$/);
            if (!match) continue;

            const fullName = match[1];
            const value = parseFloat(match[2]);
            if (isNaN(value)) continue;

            // Базовое имя без labels (для агрегации)
            const baseName = fullName.includes('{') ? fullName.replace(/\{[^}]*\}/, '').trim() : fullName;

            if (result[baseName] === undefined) {
                result[baseName] = value;
            } else {
                // Для counter-подобных метрик (total, _total) — суммируем; для gauge — берём последнее
                if (baseName.endsWith('_total') || baseName.includes('counter')) {
                    result[baseName] += value;
                } else {
                    result[baseName] = value;
                }
            }

            // Сохраняем и полное имя (с labels) для специфичных метрик
            result[fullName] = value;
        }

        return result;
    },

    /**
     * Получить значение метрики по имени (с учётом labels или без).
     * Сначала ищет точное совпадение, потом baseName.
     */
    get(data, metricName) {
        if (data[metricName] !== undefined) return data[metricName];
        const base = metricName.replace(/\{[^}]*\}/, '').trim();
        return data[base];
    },

    /**
     * Суммирует все метрики, начинающиеся с prefix (например tradingbot_messages_total с разными labels).
     */
    sumByPrefix(data, prefix) {
        let sum = 0;
        for (const [key, value] of Object.entries(data)) {
            const base = key.replace(/\{[^}]*\}/, '').trim();
            if (base === prefix || base.startsWith(prefix + '{')) {
                sum += typeof value === 'number' ? value : parseFloat(value) || 0;
            }
        }
        return sum;
    }
};
