export interface ParsedMailto {
  address: string
  parameterNames: string[]
  parameters: Record<string, string>
}

const mailtoPrefix = 'mailto:'

export function parseMailto(href: string): ParsedMailto {
  if (!href.startsWith(mailtoPrefix)) {
    throw new Error(`Expected a link that starts with "${mailtoPrefix}", found "${href}"`)
  }
  const rest = href.slice(mailtoPrefix.length)
  const questionMark = rest.indexOf('?')
  const address = questionMark === -1 ? rest : rest.slice(0, questionMark)
  const query = questionMark === -1 ? '' : rest.slice(questionMark + 1)
  const pairs = query === '' ? [] : query.split('&')
  const decodedPairs = pairs.map((pair): [string, string] => {
    const equalsSign = pair.indexOf('=')
    if (equalsSign === -1) {
      throw new Error(`Expected "name=value" in the mailto link, found "${pair}" in "${href}"`)
    }
    return [decodeURIComponent(pair.slice(0, equalsSign)), decodeURIComponent(pair.slice(equalsSign + 1))]
  })
  return {
    address,
    parameterNames: decodedPairs.map(([name]) => name),
    parameters: Object.fromEntries(decodedPairs),
  }
}
