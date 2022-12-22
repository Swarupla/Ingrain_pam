import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PstUseCaseDefinitionComponent } from './pst-use-case-definition.component';

describe('PstUseCaseDefinitionComponent', () => {
  let component: PstUseCaseDefinitionComponent;
  let fixture: ComponentFixture<PstUseCaseDefinitionComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PstUseCaseDefinitionComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PstUseCaseDefinitionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
