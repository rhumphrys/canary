import React, { Component } from 'react';
import _ from 'lodash';
import { Property } from './Property';
import { Header, Divider } from 'semantic-ui-react';

export class Category extends Component {
  displayName = Category.name;

  constructor(props) {
    super(props);
    this.state = { ...this.props };
    this.updateCategory = this.updateCategory.bind(this);
  }

  updateCategory(path, value) {
    var category = { ...this.state.category };
    _.set(category, path, value);
    this.setState({ category: category }, () => {
      this.props.updateFhirInfo(this.props.name, this.state.category);
    });
  }

  render() {
    return (
      <React.Fragment>
        <Divider horizontal>
          <Header size="huge">{this.props.name}</Header>
        </Divider>
        {Object.keys(this.props.category).map(name => {
          return (
            <Property
              key={name}
              lookup={name}
              name={this.props.category[name].Name}
              property={this.props.category[name]}
              updateCategory={this.updateCategory}
              editable={this.props.editable}
              hideSnippets={this.props.hideSnippets}
              testMode={this.props.testMode}
            />
          );
        })}
      </React.Fragment>
    );
  }
}
