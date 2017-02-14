<xsl:stylesheet
  xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:local="local"
  exclude-result-prefixes="local xs"
  version="3.0">

  <xsl:output method="xml"/>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="input[xs:integer(@number) eq 1]" name="named-template">
    <xsl:param name="number" as="xs:integer" select="xs:integer(@number) * 10"/>
    <output number="{$number}"/>
  </xsl:template>

  <xsl:template match="input" mode="non-default">
    <output mode="non-default"/>
  </xsl:template>

  <xsl:template match="input" mode="filter-element">
    <only-i-matter right="?">
      <no-one-cares-about-me/>
    </only-i-matter>
  </xsl:template>

  <xsl:template match="input" mode="generate-id">
    <output id="{generate-id()}"/>
  </xsl:template>

  <xsl:template match="input">
    <output/>
  </xsl:template>

  <xsl:template match="nothing"/>

  <xsl:function name="local:reverse" as="xs:string">
    <xsl:param name="str" as="xs:string"/>

    <xsl:sequence select="
        codepoints-to-string(reverse(string-to-codepoints($str)))
    "/>
  </xsl:function>

  <xsl:function name="local:rename-attribute" as="attribute()">
    <xsl:param name="attr" as="attribute()"/>

    <xsl:variable name="el" as="element()">
      <el>
        <xsl:attribute name="bar" select="$attr"/>
      </el>
    </xsl:variable>

    <xsl:sequence select="$el/@bar"/>
  </xsl:function>

  <xsl:template match="parent/child">
    <output number="{../@number}">ignore me</output>
  </xsl:template>

</xsl:stylesheet>
